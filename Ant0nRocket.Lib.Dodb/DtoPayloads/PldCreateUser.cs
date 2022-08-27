using System.ComponentModel.DataAnnotations;

using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Entities;

namespace Ant0nRocket.Lib.Dodb.DtoPayloads
{
    /// <summary>
    /// Payload for creating a new user.
    /// </summary>
    public class PldCreateUser : DtoPayloadBase
    {
        /// <summary>
        /// Default <see cref="User"/> value.
        /// </summary>
        public User Value { get; set; } = new();
    }
}
