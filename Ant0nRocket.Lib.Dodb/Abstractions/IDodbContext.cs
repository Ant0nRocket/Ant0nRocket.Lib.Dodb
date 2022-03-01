using Ant0nRocket.Lib.Dodb.Entities;

using Microsoft.EntityFrameworkCore;

namespace Ant0nRocket.Lib.Dodb.Abstractions
{
    public interface IDodbContext
    {
        DbSet<Document> Documents { get; set; }
    }
}
