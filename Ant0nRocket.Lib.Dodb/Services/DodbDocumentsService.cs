using Ant0nRocket.Lib.Dodb.Entities;
using Ant0nRocket.Lib.Dodb.Gateway;
using Ant0nRocket.Lib.Std20.Logging;

using Microsoft.EntityFrameworkCore;

namespace Ant0nRocket.Lib.Dodb.Services
{
    public static class DodbDocumentsService
    {
        private static readonly Logger _logger = Logger.Create(nameof(DodbDocumentsService));

        /// <summary>
        /// Checks any document (default <paramref name="documentId"/>) or
        /// specified by <paramref name="documentId"/> exists.
        /// </summary>
        public static bool CheckDocumentExist(Guid documentId = default)
        {
            using var dbContext = DodbGateway.GetDbContext();
            var query = dbContext.Documents.AsNoTracking();

            if (documentId != default)
                query = query.Where(d => d.Id == documentId);

            return query.Any();
        }

        /// <summary>
        /// Gets the latest document Id or empty Guid if no documents found.
        /// </summary>
        public static Guid GetLatestDocumentId()
        {
            using var dbContext = DodbGateway.GetDbContext();
            return dbContext
                    .Documents
                    .AsNoTracking()
                    .OrderByDescending(d => d.DateCreatedUtc)
                    .FirstOrDefault()?.Id ?? Guid.Empty;
        }

        /// <summary>
        /// Retreives the <see cref="Document"/> specified by <paramref name="documentId"/>.<br />
        /// <paramref name="excludePayload"/> could be set to true if you know that payload 
        /// is very heavy and there is no need to read it from database.
        /// </summary>
        public static Document? GetDocument(Guid documentId, bool excludePayload = false)
        {
            using var dbContext = DodbGateway.GetDbContext();
            var query = dbContext
                .Documents
                .AsNoTracking()
                .Where(d => d.Id == documentId);

            if (excludePayload)
                query = query.Select(d => new Document
                {
                    Id = d.Id,
                    UserId = d.UserId,
                    RequiredDocumentId = d.RequiredDocumentId,
                    Description = d.Description,
                    PayloadType = d.PayloadType,
                    DateCreatedUtc = d.DateCreatedUtc,
                });

            return query.SingleOrDefault();
        }
    }
}
