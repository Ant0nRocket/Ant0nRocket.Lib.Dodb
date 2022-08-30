using System.Collections.Generic;

using Ant0nRocket.Lib.Dodb.Models;

namespace Ant0nRocket.Lib.Dodb.Tests.Model
{
    /// <summary>
    /// Basic user implementation.
    /// </summary>
    public class User : EntityBase
    {
        public string? Name { get; set; }

        public string? PasswordHash { get; set; }

        public bool IsAdmin { get; set; }

        public bool IsHidden { get; set; }

        public List<Document>? Documents { get; set; }
    }
}
