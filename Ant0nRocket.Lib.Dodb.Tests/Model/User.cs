using System.Collections.Generic;

using Ant0nRocket.Lib.Dodb.Models;

namespace Ant0nRocket.Lib.Dodb.Tests.Model
{
    /// <summary>
    /// Basic user implementation.
    /// </summary>
    public class User : EntityBase
    {
        /// <summary>
        /// User name (try not pass here any novels :)
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Hash of a password.
        /// </summary>
        public string? PasswordHash { get; set; }

        public bool IsAdmin { get; set; }

        public bool IsHidden { get; set; }

        public List<Document>? Documents { get; set; }
    }
}
