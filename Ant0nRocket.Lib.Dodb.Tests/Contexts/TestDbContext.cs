using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Entities;
using Ant0nRocket.Lib.Std20.IO;
using Ant0nRocket.Lib.Std20.Logging;

using Microsoft.EntityFrameworkCore;

namespace Ant0nRocket.Lib.Dodb.Tests.Contexts
{
    public class TestDbContext : DbContext, IDodbContext
    {
        private static Logger _logger = Logger.Create(nameof(TestDbContext));

        public DbSet<Document> Documents { get; set; }

        public DbSet<PayloadType> PayloadTypes { get; set; }

        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var fileName = FileSystemUtils
                .GetDefaultAppDataFolderPathFor("TestDb.sqlite", subDirectory: "Data", autoTouchDirectory: true);
            optionsBuilder.UseSqlite($"Filename='{fileName}'");
            optionsBuilder.EnableSensitiveDataLogging(true);
            optionsBuilder.LogTo(_logger.LogEF);
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
