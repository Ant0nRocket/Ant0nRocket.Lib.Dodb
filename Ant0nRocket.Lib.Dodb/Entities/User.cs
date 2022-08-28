using System.ComponentModel.DataAnnotations;

namespace Ant0nRocket.Lib.Dodb.Entities
{
    /// <summary>
    /// Basic user implementation. If you need more advanced logic - 
    /// fill free to inherit this class in your namespace.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Id of a user.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

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

        /// <summary>
        /// Is current user an admin
        /// </summary>
        public bool IsAdmin { get; set; } = false;

        /// <summary>
        /// Is current user hidden (on GUI)
        /// </summary>
        public bool IsHidden { get; set; } = false;

        /// <summary>
        /// Is current user deleted/disabled
        /// </summary>
        public bool IsDeleted { get; set; } = false;
    }
}
