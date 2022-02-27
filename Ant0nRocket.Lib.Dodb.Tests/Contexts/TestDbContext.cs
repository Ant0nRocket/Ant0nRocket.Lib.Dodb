using Ant0nRocket.Lib.Dodb.Contexts;
using Ant0nRocket.Lib.Std20.DependencyInjection.Attributes;
using Ant0nRocket.Lib.Std20.IO;

using Microsoft.EntityFrameworkCore;

namespace Ant0nRocket.Lib.Dodb.Tests.Contexts
{
    [Transient]
    internal class TestDbContext : DbContextBase
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var fileName = FileSystemUtils.GetDefaultAppDataFolderPathFor("TestDb.sqlite");
            optionsBuilder.UseSqlite($"Filename='{fileName}'");
        }

        public TestDbContext()
        {
            Database.EnsureDeleted();
            Database.EnsureCreated();
        }
    }
}
