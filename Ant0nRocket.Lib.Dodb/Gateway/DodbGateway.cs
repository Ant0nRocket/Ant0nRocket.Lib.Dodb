using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Attributes;
using Ant0nRocket.Lib.Dodb.DtoPayloads;
using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Dodb.Entities;
using Ant0nRocket.Lib.Dodb.Gateway.Helpers;
using Ant0nRocket.Lib.Dodb.Gateway.Responses;
using Ant0nRocket.Lib.Dodb.Services;
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

        private static bool _isKnownPayloadTypesRegistred = false;

        private static bool? _isAnyDocumentExist = null;

        private static readonly Dictionary<string, int> _payloadType_Id_Cache = new();

        #endregion

        #region Constants

        private const string ERROR_GETTING_DBCONTEXT = $"Can't create DbContext. Check {nameof(RegisterDbContextGetterFunc)} were called.";
        private const string ERROR_NEED_REGISTER_IPAYLOADS = $"Call {nameof(RegisterKnownPayloadTypes)} to register IPayload classes";
        private const string ERROR_IPAYLOADS_REG_FAILED = "Failed to register IPayload classes";

        #endregion

        #region DbContext getter

        /*
         Why we need a DbContext getter?
         This class doesn't know about the final class you will work with but it has to be IDodbContext.
         As you DbContext will be somewhere in external library we need to know how to get it.
         */

        private static Func<IDodbContext>? dbContextGetterFunc;

        /// <summary>
        /// Returnes a DbContext with function that was registered with
        /// <see cref="RegisterDbContextGetterFunc(Func{IDodbContext})"/>.
        /// </summary>
        internal static IDodbContext? GetDbContext() =>
            dbContextGetterFunc?.Invoke();

        #endregion

        #region Registrators

        /// <summary>
        /// Library itself doesn't know about actual DbContext you will use in your
        /// application, but it know that it must implement <see cref="IDodbContext"/>.<br />
        /// So your app must register context getter.
        /// </summary>
        public static void RegisterDbContextGetterFunc(Func<IDodbContext> value) =>
            dbContextGetterFunc = value;

        /// <summary>
        /// By default lib can handle only build-in payload types.<br />
        /// Provide your own handler to be able to deal with your types.
        /// </summary>
        public static void RegisterDtoPayloadHandler(DtoPayloadHandler handler) => dtoPayloadHandler = handler;

        /// <summary>
        /// Goes through current app domain and register all classes of type IPayload
        /// in database.
        /// </summary>
        public static bool RegisterKnownPayloadTypes()
        {
            if (_isKnownPayloadTypesRegistred) return true;

            using var dbContext = GetDbContext() ??
                throw new ApplicationException($"Call {nameof(RegisterDbContextGetterFunc)} first");

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

            dbContext.PayloadTypes.ToList().ForEach(pt => {
                var typeName = pt.TypeName ?? throw new NullReferenceException();
                _payloadType_Id_Cache.Add(typeName, pt.Id);
            });

            _isKnownPayloadTypesRegistred = true;

            return true; // no changes or add success
        }

        #endregion

        #region DTO handling

        private static DtoPayloadHandler? dtoPayloadHandler = null;

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
            if (dtoPayloadHandler != null)
                return dtoPayloadHandler(dtoPayload, dbContext);
            return null;
        }

        private static GatewayResponse PushDtoObject(Dto dto, IDodbContext dbContext)
        {
            #region 1. Checking handlers and values

            if (dbContextGetterFunc == default) throw new NullReferenceException(nameof(dbContextGetterFunc));
            if (!_isKnownPayloadTypesRegistred) throw new ApplicationException(ERROR_NEED_REGISTER_IPAYLOADS);

            if (_isAnyDocumentExist == null)
            {
                _isAnyDocumentExist = DodbDocumentsService.CheckDocumentExist();
                logger.LogDebug($"Any documents found? - {_isAnyDocumentExist}");
            }

            #endregion

            #region 2. Check document doesn't exists, check required document exists

            // Check user exists (if it's not first document!)
            if (!DodbUsersService.CheckUserExists(dto.UserId)) // user NOT found
            {
                if (_isAnyDocumentExist == true) // ...but some documents already exists
                {
                    logger.LogError($"DTO from unknown user '{dto.UserId}' received");
                    return new GrDtoFromUnknownUser { UserId = dto.UserId };
                }
            }

            // Check document with dto.Id doesn't exist
            if (DodbDocumentsService.CheckDocumentExist(dto.Id))
            {
                logger.LogError($"Document with Id='{dto.Id}' already exists");
                return new GrDocumentExists { DocumentId = dto.Id };
            }

            if (dto.RequiredDocumentId == default)
            {
                if (dto.DateCreatedUtc == DateTime.MinValue) // that means in app generated
                {
                    dto.DateCreatedUtc = DateTime.UtcNow;
                    dto.RequiredDocumentId = DodbDocumentsService.GetLatestDocumentId();
                }
                else
                {
                    if (_isAnyDocumentExist == true) // database is NOT empty
                    {                                // but someone send to us DTO with no RequiredDocId but with date
                        var message = $"Invalid DTO '{dto.Id}' with no date and no requied document ID received";
                        logger.LogError(message);
                        return new GrDtoIsInvalid(message);
                    }
                }
            }
            else // if (dto.RequiredDocumentId == default) || some required document specified in DTO
            {
                if (dto.DateCreatedUtc == DateTime.MinValue)
                {
                    var message = $"Invalid DTO '{dto.Id}' with no date but with required document ID received";
                    logger.LogError(message);
                    return new GrDtoIsInvalid(message);
                }

                if (!DodbDocumentsService.CheckDocumentExist(dto.RequiredDocumentId))
                {
                    logger.LogWarning($"DTO '{dto.Id}' requires Document '{dto.RequiredDocumentId}' which is not found");
                    return new GrRequiredDocumentNotFound
                    {
                        RequesterId = dto.Id,
                        RequiredDocumentId = dto.RequiredDocumentId,
                    };
                }
            }

            #endregion

            #region Handling DTO, saving document

            var dtoType = dto.GetType();
            var dtoPayloadPropertyInfo = dtoType.GetProperties().FirstOrDefault(p => p.Name == "Payload") ??
                throw new ArgumentException("DTO doesn't have a Payload property");
            var dtoPayload = dtoPayloadPropertyInfo.GetValue(dto) ??
                throw new InvalidDataException("Can't get a value of Dto.Payload");

            if (_isAnyDocumentExist == false && dtoPayload is PldCreateUser p)
            {
                dto.UserId = p.Value.Id;
                logger.LogWarning($"Created user will be an author of a document, 'cause it's a first document!");
            }

            var dtoHandleResponse = // first, try internal handler, and if not (it will return null) drop to registred
                TryHandleDtoPayloadInternally(dtoPayload, dbContext) ??
                TryHandleDtoPayloadExternally(dtoPayload, dbContext) ??
                new GrDtoPayloadHandlerNotFound();

            var isDtoHandledSuccessfully = AttributeUtils
                .GetAttribute<IsSuccessAttribute>(dtoHandleResponse.GetType())?.IsSuccess ?? true;

            if (isDtoHandledSuccessfully == false)
            {
                logger.LogError($"Got {dtoHandleResponse.GetType().Name} for DTO '{dto.Id}': " +
                    $"{dtoHandleResponse.AsJson()}");
                return dtoHandleResponse;
            }

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

            #endregion

            return dtoHandleResponse;
        }

        private static int GetPayloadTypeId(object dtoPayload)
        {
            var typeName = dtoPayload.GetType().FullName ?? string.Empty;
            if (_payloadType_Id_Cache.ContainsKey(typeName))
                return _payloadType_Id_Cache[typeName];
            return int.MinValue;
        }

        /// <summary>
        /// 1. Throws <see cref="NullReferenceException"/> if you forgot to register ContextGetter (see <see cref="RegisterDbContextGetterFunc(Func{IDodbContext})"/>).<br /> 
        /// 2. Throws <see cref="NullReferenceException"/> if you forgot to register DtoHandler (see <see cref="RegisterDtoPayloadHandler(DtoPayloadHandler)"/>).<br />
        /// 3. Returnes <see cref="GrDtoIsInvalid"/> if there are some validation errors in DTO or its payload.<br />
        /// 4. Returnes <see cref="GrDocumentExists"/> if any document with <paramref name="dto"/>.Id already exists.<br />
        /// 5. Returnes <see cref="GrRequiredDocumentNotFound"/> if there is some document required to exist but not found.<br />
        /// 6. Returnes <see cref="GrDtoPayloadHandlerNotFound"/> if there is no handler found for payload.<br />
        /// 7. Returnes <see cref="GrPushDtoFailed"/> if there some errors durring commit.<br />
        /// <br />
        /// Othervise returnes some <see cref="GatewayResponse"/><br />
        /// ------------------------<br />
        /// If need to prevent authToken validation (say, in SyncService) set <paramref name="skipAuthTokenValidation"/>
        /// to true.<br />
        /// If <paramref name="externalDbContext"/> passed then all transaction control, saving, disposing - is not 
        /// a business of current function. If you need just push DTO and commit it - dont set <paramref name="externalDbContext"/>!
        /// </summary>
        public static GatewayResponse PushDto(Dto dto, IDodbContext? externalDbContext = default)
        {
            var validator = new DtoValidator(dto).Validate();
            if (validator.HasFoundErrors)
            {
                logger.LogError($"Invalid DTO '{dto.Id}': {string.Join(", ", validator.ErrorsList)}");
                return new GrDtoIsInvalid(validator.ErrorsList);
            }

            var dbContext = externalDbContext ?? GetDbContext() ??
                throw new NullReferenceException(ERROR_GETTING_DBCONTEXT);

            var pushResult = PushDtoObject(dto, dbContext);

            if (externalDbContext == default)
            {
                try
                {
                    dbContext?.SaveChanges();
                }
                catch (Exception ex)
                {
                    var message = $"{ex.Message} " + ex.InnerException?.Message ?? string.Empty;
                    pushResult = new GrPushDtoFailed { Message = message };
                    logger.LogException(ex, $"Unable to proceed DTO '{dto.Id}'");
                }
                finally
                {
                    dbContext?.Dispose();
                }
            }

            return pushResult;
        }

        #endregion

        #region Internal helper functions

        /// <summary>
        /// Determines whether DodbGateway know about type with FullName=<paramref name="typeName"/>.<br />
        /// <b>N.B.!</b> "Know about" and "can handle" are two different things!<br />
        /// In our case "know about" means that type with <paramref name="typeName"/> exists somewhere
        /// in current app domain.
        /// </summary>
        internal static bool CanGatewayHandlePayloadType(string typeName) => 
            _payloadType_Id_Cache.ContainsKey(typeName);

        #endregion
    }
}
