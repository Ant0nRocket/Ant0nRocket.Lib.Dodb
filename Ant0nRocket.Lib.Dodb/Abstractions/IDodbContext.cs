using Ant0nRocket.Lib.Dodb.Entities;

using Microsoft.EntityFrameworkCore;

namespace Ant0nRocket.Lib.Dodb.Abstractions
{
    /// <summary>
    /// Basic DbContext interface. Lib works only with
    /// intances that implements it.<br />
    /// All consumers of the lib should create their contexts
    /// using this interface.
    /// </summary>
    public interface IDodbContext : IDisposable
    {
        /// <summary>
        /// Set of <see cref="Entities.Document"/>
        /// </summary>
        DbSet<Document> Documents { get; set; }

        /// <summary>
        /// Payload types with ordinal Id.
        /// </summary>
        DbSet<PayloadType> PayloadTypes { get; set; }

        /// <summary>
        /// Set of <see cref="Entities.User"/>
        /// </summary>
        DbSet<User> Users { get; set; }

        /// <summary>
        /// Bypass to real DbContext.SaveChanges() function.
        /// </summary>
        int SaveChanges();
    }
}
