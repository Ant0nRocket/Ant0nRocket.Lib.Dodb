using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Entities;

using Microsoft.EntityFrameworkCore;

namespace Ant0nRocket.Lib.Dodb.DbContexts
{
    public abstract class DodbContextBase : DbContext, IDodbContext
    {
        public virtual DbSet<Document> Documents { get; set; }
        public virtual DbSet<PayloadType> PayloadTypes { get; set;}
        public virtual DbSet<User> Users { get; set;}

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<User>().HasOne(u => u.RecordCreator).WithOne(d => d.User).HasForeignKey("Document.Id");
        }
    }
}
