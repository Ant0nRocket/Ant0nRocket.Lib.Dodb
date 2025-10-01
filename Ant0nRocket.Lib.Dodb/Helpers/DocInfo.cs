using Ant0nRocket.Lib.Dodb.Models;

namespace Ant0nRocket.Lib.Dodb.Helpers
{
    /// <summary>
    /// Short information about the <see cref="Document"/>.
    /// </summary>
    public class DocInfo
    {
        /// <summary>
        /// Id of a <see cref="Document"/>
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// <see cref="Document"/> creation date
        /// </summary>
        public DateTime DateCreatedUtc { get; set; }
    }
}
