using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Entities;
using Ant0nRocket.Lib.Std20.IO;

using Microsoft.EntityFrameworkCore;

namespace Ant0nRocket.Lib.Dodb.Tests.Contexts
{
    public class TestDbContext : DbContext, IDodbContext
    {
        public DbSet<Document> Documents { get; set; }

        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var fileName = FileSystemUtils.GetDefaultAppDataFolderPathFor("TestDb.sqlite");
            optionsBuilder.UseSqlite($"Filename='{fileName}'");
        }

        private static bool databaseRecteated = false;

        public TestDbContext()
        {
            if (!databaseRecteated)
            {
                Database.EnsureDeleted();
                Database.EnsureCreated();
                databaseRecteated = true;
            }
        }
    }
}
