using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Dodb.Entities;
using Ant0nRocket.Lib.Dodb.Gateway.Responces;
using Ant0nRocket.Lib.Std20.Extensions;
using Ant0nRocket.Lib.Std20.Logging;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ant0nRocket.Lib.Dodb.Gateway.Helpers
{
    internal class Dto2DocumentConverter : IDisposable
    {
        private readonly Logger logger = Logger.Create<Dto2DocumentConverter>();

        private IDodbContext dbContext;
        private IDbContextTransaction transaction;
        private Document document;
        private Dto dto;

        public Dto2DocumentConverter WithDbContext(IDodbContext dbContext)
        {
            this.dbContext = dbContext;
            transaction = (this.dbContext as DbContext).Database.BeginTransaction();
            return this;
        }

        public Dto2DocumentConverter CreateDocumentFrom<T>(DtoOf<T> dto) where T : class, new()
        {
            if (dbContext == default) throw new NullReferenceException(nameof(dbContext));

            // New DTO has default values, so fill them and continue.
            if (dto.DateCreatedUtc == DateTime.MinValue)
            {
                dto.RequiredDocumentId = dbContext
                    .Documents.AsNoTracking()
                    .OrderByDescending(d => d.DateCreatedUtc)
                    .FirstOrDefault()?.Id ?? Guid.Empty;
                dto.DateCreatedUtc = DateTime.UtcNow;
            }

            document = new Document
            {
                Id = dto.Id,
                AuthorId = dto.AuthorId,
                RequiredDocumentId = dto.RequiredDocumentId,
                DateCreatedUtc = dto.DateCreatedUtc,
                PayloadType = $"{dto.Payload.GetType()}",
                Payload = dto.Payload.AsJson()
            };

            dbContext.Documents.Add(document);

            this.dto = dto;

            return this;
        }


        public GatewayResponse AndHandleDtoWith(DtoHandler handler)
        {
            if (document == default) throw new NullReferenceException(nameof(document));
            if (dbContext == default) throw new NullReferenceException(nameof(dbContext));
            if (handler == default) throw new ArgumentNullException(nameof(handler));

            if (dbContext.Documents.AsNoTracking().Where(d => d.Id == document.Id).Any())
                return new GrDocumentExists { DocumentId = document.Id };

            if (document.RequiredDocumentId != default)
                if (!dbContext.Documents.AsNoTracking().Any(d => d.Id == document.RequiredDocumentId))
                    return new GrRequiredDocumentNotFound
                    {
                        RequesterId = document.Id,
                        RequiredDocumentId = document.RequiredDocumentId
                    };

            try
            {
                var handleResult = handler?.Invoke(dto as DtoOf<object>, dbContext);
                if (handleResult is GrDtoSaveSuccess)
                {
                    transaction.Commit();
                    logger.LogInformation($"Document '{document.Id}' saved");
                }
                else
                {
                    transaction.Rollback();
                    logger.LogError($"Error while saving document '{document.Id}': got {handleResult.GetType().Name}");
                }
                return handleResult;
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                return new GrDtoSaveFailed { DocumentId = document.Id };
            }
        }

        public void Dispose()
        {
            transaction.Dispose();
        }
    }
}
