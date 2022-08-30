using System.ComponentModel.DataAnnotations;

using Ant0nRocket.Lib.Dodb.Dto.Payloads;

namespace Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads
{
    public class PldCreateUser : DtoPayloadBase
    {
        /// <summary>
        /// User name (try not pass here any novels :)
        /// </summary>
        [Required]
        [MinLength(1)]
        public string? Name { get; set; }

        /// <summary>
        /// Hash of a password.
        /// </summary>
        [Required]
        public string? PasswordHash { get; set; }

        public bool IsAdmin { get; set; }

        public bool IsHidden { get; set; }
    }
}
