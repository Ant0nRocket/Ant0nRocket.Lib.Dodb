using Ant0nRocket.Lib.Dodb.Entities;

using Microsoft.EntityFrameworkCore;

namespace Ant0nRocket.Lib.Dodb.Abstractions
{
    public interface IDodbContext : IDisposable
    {
        DbSet<Document> Documents { get; set; }

        DbSet<User> Users { get; set; }

        int SaveChanges();
    }
}
