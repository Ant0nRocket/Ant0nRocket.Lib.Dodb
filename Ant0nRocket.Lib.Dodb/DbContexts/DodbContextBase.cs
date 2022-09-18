using Ant0nRocket.Lib.Dodb.Models;

using Microsoft.EntityFrameworkCore;

namespace Ant0nRocket.Lib.Dodb.DbContexts
{
    /// <summary>
    /// Base DbContext for Dodb database.
    /// </summary>
    public abstract class DodbContextBase : DbContext
    {
        /// <summary>
        /// All documents that were applied.
        /// </summary>
        public DbSet<Document> Documents { get; set; }

        /// <summary>
        /// All known payload types of AppDomain.
        /// </summary>
        public DbSet<DocumentPayload> DocumentPayloads { get; set;}

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Document>().HasIndex(p => p.PayloadTypeName).IsUnique(false);
        }
    }
}
