using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Attributes;
using Ant0nRocket.Lib.Dodb.DbContexts;
using Ant0nRocket.Lib.Dodb.DtoPayloads;
using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Dodb.Entities;
using Ant0nRocket.Lib.Dodb.Gateway.Helpers;
using Ant0nRocket.Lib.Dodb.Gateway.Responses;
using Ant0nRocket.Lib.Dodb.Services;
using Ant0nRocket.Lib.Dodb.Services.Responces.DodbUsersService;
using Ant0nRocket.Lib.Std20.Cryptography;
using Ant0nRocket.Lib.Std20.Extensions;
using Ant0nRocket.Lib.Std20.Logging;
using Ant0nRocket.Lib.Std20.Reflection;

using Microsoft.EntityFrameworkCore;

namespace Ant0nRocket.Lib.Dodb.Gateway
{
    /// <summary>
    /// A gateway for document-oriented database.<br />
    /// Before using this in your project execute:
    /// <code>
    ///     DodbGateway.RegisterDbContextGetterFunc([...function that will return DbContext...]);
    ///     DodbGateway.RegisterKnownPayloadTypes();
    ///     DodbGateway.RegisterDtoPayloadHandler([...Method that will handle your Dto objects...]);
    /// </code>
    /// </summary>
    public static class DodbGateway
    {
        #region Private fields

        private static readonly Logger logger = Logger.Create(nameof(DodbGateway));

        private static bool _isInitialized = false;

        private static readonly Dictionary<string, int> _payloadType_Id_Cache = new();

        #endregion

        #region Constants

        private const string ERROR_GETTING_DBCONTEXT = $"Can't create DbContext. Check {nameof(Initialize)} were called with non-null args.";
        private const string ERROR_GETTING_PASSWORD_HASHER = $"Can't get password hasher. Check {nameof(Initialize)} were called with non-null args";
        private const string ERROR_NOT_INITIALIZED = $"Call {nameof(Initialize)} before using {nameof(DodbGateway)}";

        private const string ERROR_NEED_REGISTER_IPAYLOADS = $"Call {nameof(RegisterKnownPayloadTypes)} to register IPayload classes";
        private const string ERROR_IPAYLOADS_REG_FAILED = "Failed to register IPayload classes";

        #endregion

        #region DbContext handler

        /*
         Why we need a DbContext getter?
         This class doesn't know about the final class you will work with but it has to be IDodbContext.
         As you DbContext will be somewhere in external library we need to know how to get it.
         */

        private static GetDbContextHandler? _getDbContextHandler = null;

        /// <summary>
        /// Returnes a DbContext using <see cref="GetDbContextHandler"/> that was registered with
        /// <see cref="Initialize(GetDbContextHandler, DtoPayloadHandler, GetPasswordHashHandler)"/>.
        /// </summary>
        internal static DodbContextBase GetDbContext() =>
            _getDbContextHandler?.Invoke() ?? throw new ApplicationException(ERROR_GETTING_DBCONTEXT);

        #endregion

        #region DTO payload handler

        private static DtoPayloadHandler? _dtoPayloadHandler = null;

        #endregion

        #region Password hash handler

        private static GetPasswordHashHandler _getPasswordHashHandler = DefaultPasswordHashHandler;

        private static string DefaultPasswordHashHandler(string plainPassword)
        {
            return Hasher.ComputeHash(plainPassword).ToHexString();
        }

        internal static GetPasswordHashHandler GetPasswordHashHandler() =>
            _getPasswordHashHandler ?? throw new ApplicationException(ERROR_GETTING_PASSWORD_HASHER);

        #endregion

        #region Initialization        

        /// <summary>
        /// Performes initialization of a library.
        /// </summary>
        public static void Initialize(
            GetDbContextHandler getDbContextHandler,
            DtoPayloadHandler dtoPayloadHandler,
            GetPasswordHashHandler? getPasswordHashHandler = null)
        {
            if (_isInitialized) return;

            _getDbContextHandler = getDbContextHandler ?? throw new NullReferenceException(nameof(getDbContextHandler));
            _dtoPayloadHandler = dtoPayloadHandler ?? throw new NullReferenceException(nameof(dtoPayloadHandler));

            if (getPasswordHashHandler == null)
            {
                logger.LogInformation("Password hasher function wasn't provided. Internal function will be used.");
                _getPasswordHashHandler = DefaultPasswordHashHandler;
            }
            else
            {
                logger.LogInformation("Password hasher function was provided. External function will be used.");
                _getPasswordHashHandler = getPasswordHashHandler;
            }

            RegisterKnownPayloadTypes();

            // Important to set this flag here! TouchUser will throw if false.
            _isInitialized = true;

            TouchRootUser();
        }

        #endregion

        #region DTO handling

        private static GatewayResponse? TryHandleDtoPayloadInternally(object dtoPayload, IDodbContext dbContext)
        {
            var c = dbContext;
            return dtoPayload switch
            {
                PldCreateUser p => DodbUsersService.CreateUser(p, c),
                _ => null
            };
        }

        private static GatewayResponse? TryHandleDtoPayloadExternally(object dtoPayload, IDodbContext dbContext)
        {
            if (_dtoPayloadHandler != null)
                return _dtoPayloadHandler(dtoPayload, dbContext);
            return null;
        }

        /// <summary>
        /// Function only tryes to apply <paramref name="dto"/> inside <paramref name="dbContext"/>.<br />
        /// It doesn't check trnsactions, doesn't valid, only handling!
        /// </summary>
        private static GatewayResponse PushDtoObject(Dto dto, IDodbContext dbContext)
        {
            var dtoType = dto.GetType();
            var dtoPayloadPropertyInfo = dtoType.GetProperties().FirstOrDefault(p => p.Name == "Payload") ??
                throw new ArgumentException("DTO doesn't have a Payload property");
            var dtoPayload = dtoPayloadPropertyInfo.GetValue(dto) ??
                throw new InvalidDataException("Can't get a value of Dto.Payload");

            var dtoHandleResponse = // first, try internal handler, then try external handler, or drop no handler found
                TryHandleDtoPayloadInternally(dtoPayload, dbContext) ??
                TryHandleDtoPayloadExternally(dtoPayload, dbContext) ??
                new GrDtoPayloadHandlerNotFound();

            var document = new Document
            {
                Id = dto.Id,
                UserId = dto.UserId,
                RequiredDocumentId = dto.RequiredDocumentId,
                DateCreatedUtc = dto.DateCreatedUtc,
                Description = dto.Description,
                PayloadTypeId = GetPayloadTypeId(dtoPayload),
                PayloadJson = dtoPayload.AsJson()
            };

            dbContext.Documents.Add(document);

            logger.LogInformation($"[uncommited] DTO '{dto.Id}' applied");

            return dtoHandleResponse;
        }

        /// <summary>
        /// 1. Throws <see cref="ApplicationException"/> if library wasn't initialized (see <see cref="Initialize(GetDbContextHandler, DtoPayloadHandler, Gateway.GetPasswordHashHandler?)"/>.<br />
        /// 2. Returnes <see cref="GrDtoIsInvalid"/> if there are some validation errors in DTO or its payload.<br />
        /// 3. Returnes <see cref="GrDtoFromUnknownUser"/> if no user found by <see cref="Dto.UserId"/>.<br />
        /// 4. Returnes <see cref="GrDocumentExists"/> if any document with <paramref name="dto"/>.Id already exists.<br />
        /// 5. Returnes <see cref="GrRequiredDocumentNotFound"/> if there is some document required to exist but not found.<br />
        /// 6. Returnes <see cref="GrDtoPayloadHandlerNotFound"/> if there is no handler found for payload.<br />
        /// 7. Returnes <see cref="GrPushDtoFailed"/> if there some errors durring commit.<br />
        /// <br />
        /// Othervise returnes some <see cref="GatewayResponse"/><br />
        /// ------------------------<br />
        /// If <paramref name="externalDbContext"/> passed then all transaction control, saving, disposing - is not 
        /// a business of current function. If you need just push DTO and commit it - dont set <paramref name="externalDbContext"/>!
        /// </summary>
        public static GatewayResponse PushDto(Dto dto, DodbContextBase? externalDbContext = default)
        {
            if (!_isInitialized) throw new ApplicationException(ERROR_NOT_INITIALIZED);

            #region Basic validation (will check properties according to annotations)

            var validator = new DtoValidator(dto).Validate();
            if (validator.ValidationResults.Count > 0)
            {
                logger.LogError($"Invalid DTO '{dto.Id}': {string.Join(", ", validator.ErrorsList)}");
                return new GrDtoIsInvalid(validator.ErrorsList);
            }

            #endregion

            // ... ok, basic validation passed, let's go to database and check few more thing.
            // It's time to create a DbContext here.
            var dbContext = externalDbContext ?? GetDbContext(); // COULD BE EXTERNAL !!!

            #region Database validations (will check UserId, RequiredDocumentId exists)

            if (dto.UserId == null)
            {
                if (dbContext.Documents.Any())
                {
                    var message = $"Invalid DTO received: UserId not specified";
                    logger.LogError(message);
                    return new GrDtoIsInvalid(message);
                }
            }
            else // something is set in UserId
            {
                if (dbContext.Users.Any(u => u.Id == dto.UserId) == false)
                {
                    logger.LogWarning($"DTO from unknown user '{dto.UserId}' received");
                    return new GrDtoFromUnknownUser { UserId = dto.UserId };
                }
            }

            if (dbContext.Documents.Any(d => d.Id == dto.Id))
            {
                logger.LogWarning($"Can't apply DTO '{dto.Id}': document with this Id already exists");
                return new GrDocumentExists { DocumentId = dto.Id };
            }

            if (dto.RequiredDocumentId != null)
            {
                if (!dbContext.Documents.Any(d => d.Id == dto.RequiredDocumentId))
                {
                    logger.LogWarning($"Can't apply DTO '{dto.Id}': required document '{dto.RequiredDocumentId}' doesn't exists");
                    return new GrRequiredDocumentNotFound { RequesterId = dto.Id, RequiredDocumentId = dto.RequiredDocumentId };
                }
            }
            else // RequiredDocumentId is NOT specified
            {
                // This situation could be ONLY when there is a first document. 
                // So, if any document exists there should not be a DTO without RequiredDocumentId
                if (dbContext.Documents.Any())
                {
                    logger.LogError($"DTO '{dto.Id}' must have RequiredDocumentId");
                    return new GrRequiredDocumentNotSpecified { DtoId = dto.Id };
                }
            }

            #endregion

            // Alright! All checks done, DTO is ready to be applyied. But what about transaction?
            // If context is not external - let's start a transaction...
            using var transaction = externalDbContext == default ? dbContext.Database.BeginTransaction() : null;

            // ... and when transaction starter (or not :)) - push dto deeper.
            var pushResult = PushDtoObject(dto, dbContext);

            if (externalDbContext == default)
            {
                // What is going on here?
                // Very simple! If 'externalDbContext' is null means that we have create our dbContext
                // here (in this function). If so - we have a right (duty? :)) to save changes and
                // dispose what we have done.

                // By default IsSuccess=true.
                // Error responces marked with [IsSuccess(false)]
                var isDtoHandledSuccessfully = AttributeUtils
                    .GetAttribute<IsSuccessAttribute>(pushResult.GetType())?.IsSuccess ?? true;

                if (isDtoHandledSuccessfully == false)
                {
                    logger.LogError($"Got {pushResult.GetType().Name} for DTO '{dto.Id}': {pushResult.AsJson()}");
                    return pushResult;
                }

                try
                {
                    dbContext.SaveChanges();
                    transaction?.Commit();
                }
                catch (Exception ex)
                {
                    var message = $"{ex.Message} " + ex.InnerException?.Message ?? string.Empty;
                    pushResult = new GrPushDtoFailed { Message = message };
                    logger.LogException(ex, $"Unable to proceed DTO '{dto.Id}'");
                }
                finally
                {
                    dbContext.Dispose();
                }

                // ... but If 'externalDbContext' is NOT null then we must not do anything else, let
                // external owner of the context performs saving, disposing, etc.
            }

            return pushResult;
        }

        #endregion

        #region Private helper functions

        /// <summary>
        /// Goes through current app domain and register all classes of type IPayload
        /// in database.
        /// </summary>
        private static void RegisterKnownPayloadTypes()
        {
            _payloadType_Id_Cache.Clear();

            using var dbContext = GetDbContext();

            var knownPayloadTypesFromReflection = ReflectionUtils.GetClassesThatImplementsInterface<IPayload>();
            var knownPayloadTypesFromDatabase = dbContext
                .PayloadTypes
                .AsNoTracking()
                .Select(p => p.TypeName)
                .ToList();

            var payloadTypesToAdd = new HashSet<string>();

            foreach (var payloadTypeFromReflection in knownPayloadTypesFromReflection)
            {
                if (!knownPayloadTypesFromDatabase.Contains(payloadTypeFromReflection.FullName))
                {
                    payloadTypesToAdd.AddSecure(payloadTypeFromReflection.FullName ??
                        throw new NullReferenceException("Name of a type is null"));
                }
            }

            foreach (var typeName in payloadTypesToAdd)
                dbContext.PayloadTypes.Add(new PayloadType { TypeName = typeName });

            if (payloadTypesToAdd.Count > 0)
                if (dbContext.SaveChanges() <= 0)
                    throw new ApplicationException(ERROR_IPAYLOADS_REG_FAILED);

            dbContext.PayloadTypes.ToList().ForEach(pt =>
            {
                var typeName = pt.TypeName ?? throw new NullReferenceException();
                _payloadType_Id_Cache.Add(typeName, pt.Id);
            });
        }

        /// <summary>
        /// This function will work only one time - when database is empty.<br />
        /// It will create first <see cref="Document"/> and <see cref="User"/> ("__root").
        /// </summary>
        private static void TouchRootUser()
        {
            using var dbContext = GetDbContext();
            if (dbContext.Documents.Any()) return; // root user already touched.

            var dto = CreateDto<PldCreateUser>();
            dto.Payload.Value.Name = "__root";
            dto.Payload.Value.PasswordHash = GetPasswordHashHandler().Invoke("root");
            dto.Payload.Value.IsAdmin = true;
            dto.Payload.Value.IsHidden = true;

            using var transaction = dbContext.Database.BeginTransaction();

            var pushResult = PushDtoObject(dto, dbContext);
            if (pushResult is not GrCreateUser_Success)
            {
                var message = $"Unable to create root user, got: {pushResult.AsJson()}";
                logger.LogFatal(message);
                throw new ApplicationException(message);
            }

#if DEBUG
            logger.LogDebug($"{dbContext.ChangeTracker.DebugView.LongView}");
#endif
            dbContext.SaveChanges();
            transaction.Commit(); // without try! Let it throw if needed.
        }

        /// <summary>
        /// Returnes DTO payload type Id.
        /// </summary>
        private static int GetPayloadTypeId(object dtoPayload)
        {
            var typeName = dtoPayload.GetType().FullName ?? string.Empty;
            if (_payloadType_Id_Cache.ContainsKey(typeName))
                return _payloadType_Id_Cache[typeName];
            return int.MinValue;
        }

        #endregion

        #region Internal helper functions



        #endregion

        #region Public helper functions

        /// <summary>
        /// Function will create a DTO container for <typeparamref name="T"/> with
        /// filled <see cref="Dto.RequiredDocumentId"/>.<br />
        /// If <paramref name="userId"/> specified - then <see cref="Dto.UserId"/> will be filled.
        /// <b>N.B.!</b> Only <see cref="IPayload"/> classes valid for <typeparamref name="T"/>.
        /// </summary>
        public static DtoOf<T> CreateDto<T>(Guid? userId = default) where T : class, new()
        {
            if (!_isInitialized) throw new ApplicationException(ERROR_NOT_INITIALIZED);

            var result = new DtoOf<T>(userId);

            using var dbContext = GetDbContext();
            var latestDocumentId = dbContext
                .Documents
                .OrderByDescending(d => d.DateCreatedUtc)
                .Select(d => d.Id)
                .FirstOrDefault();

            if (latestDocumentId != default)
                result.RequiredDocumentId = latestDocumentId;

            return result;
        }

#if DEBUG
        /// <summary>
        /// Dropes existing database and creates a new one.<br />
        /// Known payload types will be recalculated.
        /// <b>FOR TESTS ONLY !!!</b>
        /// </summary>
        public static void RecreateDatabase()
        {
            if (!_isInitialized) throw new ApplicationException(ERROR_NOT_INITIALIZED);

            using var dbContext = GetDbContext();
            if (dbContext is DbContext ctx)
            {
                ctx.Database.EnsureDeleted();
                ctx.Database.EnsureCreated();
                RegisterKnownPayloadTypes();
            }
            else
            {
                throw new InvalidCastException(nameof(DbContext));
            }
        }
#endif

        #endregion

    }
}
