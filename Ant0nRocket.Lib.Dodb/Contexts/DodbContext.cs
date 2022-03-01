using Ant0nRocket.Lib.Dodb.Entities;

using Microsoft.EntityFrameworkCore;

namespace Ant0nRocket.Lib.Dodb.Contexts
{
    public class DodbContext : DbContext
    {
        public DbSet<Document> Documents { get; set; }

        public DbSet<User> Users { get; set; }
    }
}
