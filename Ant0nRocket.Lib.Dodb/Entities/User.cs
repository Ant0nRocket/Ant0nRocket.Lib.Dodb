using System.ComponentModel.DataAnnotations;

namespace Ant0nRocket.Lib.Dodb.Entities
{
    /// <summary>
    /// Basic user implementation. If you need more advanced logic - 
    /// fill free to inherit this class in your namespace.
    /// </summary>
    public class User : EntityBase
    {
        /// <summary>
        /// User name (try not pass here any novels :)
        /// </summary>
        [Required]
        [MinLength(1)]
        public string? Name { get; set; }

        /// <summary>
        /// Hash of a password. <br />
        /// See <see cref="Services.DodbUsersService.RegisterPasswordHasherFunc(Func{string, string})"/>
        /// to know how to register your own password hashing function.<br />
        /// Storing plain password in database is a very bad idea!<br />
        /// If no external hash function registred simple SHA-256 hash will be stored here.
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
    }
}
