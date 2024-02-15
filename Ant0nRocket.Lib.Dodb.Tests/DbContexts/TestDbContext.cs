using Ant0nRocket.Lib.Dodb.DbContexts;
using Ant0nRocket.Lib.Dodb.Tests.Model;
using Ant0nRocket.Lib.IO;
using Ant0nRocket.Lib.Logging;

using Microsoft.EntityFrameworkCore;

namespace Ant0nRocket.Lib.Dodb.Tests.Contexts
{
    public class TestDbContext : DodbContextBase
    {
        private static Logger _logger = Logger.Create(nameof(TestDbContext));
        private static bool _databaseRecteated = false;

        public DbSet<User> Users { get; set; }

        public TestDbContext()
        {
            if (!_databaseRecteated)
            {
                Database.EnsureDeleted();
                Database.EnsureCreated();
                _databaseRecteated = true;
            }
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            var fileName = FileSystemUtils
                .GetDefaultAppDataFolderPathFor("TestDb.sqlite", subDirectory: "Data", autoTouchDirectory: true);
            optionsBuilder.UseSqlite($"Filename='{fileName}'");
            optionsBuilder.EnableSensitiveDataLogging(true);
            //optionsBuilder.LogTo(_logger.LogEF);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

    }
}
