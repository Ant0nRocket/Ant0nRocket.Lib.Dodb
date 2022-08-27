using System.ComponentModel.DataAnnotations;

using Ant0nRocket.Lib.Dodb.Abstractions;

namespace Ant0nRocket.Lib.Dodb.DtoPayloads
{
    /// <summary>
    /// Payload for creating a new user.
    /// </summary>
    public class PldCreateUser : DtoPayloadBase, IValidateablePayload
    {
        /// <summary>
        /// Unique Id of a user.<br />
        /// By default - new GUID.
        /// </summary>
        public Guid UserId { get; set; } = Guid.NewGuid();

        /// <inheritdoc cref="Entities.User.Name"/>
        [Required]
        [MinLength(1)]
        public string Name { get; set; }

        /// <inheritdoc cref="Entities.User.PasswordHash"/>
        [Required]
        public string PasswordHash { get; set; }

        /// <inheritdoc cref="Entities.User.IsAdmin"/>
        public bool IsAdmin { get; set; } = false;

        /// <inheritdoc cref="Entities.User.IsHidden"/>
        public bool IsHidden { get; set; } = false;

        /// <inheritdoc />
        public void Validate(List<string> errorsList)
        {

        }
    }
}
