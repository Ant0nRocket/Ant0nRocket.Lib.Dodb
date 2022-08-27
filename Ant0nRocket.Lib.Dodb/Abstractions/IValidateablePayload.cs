namespace Ant0nRocket.Lib.Dodb.Abstractions
{
    /// <summary>
    /// Payload that has <see cref="Validate(List{string})"/> inside.<br />
    /// Lib will call this method dynamically if it exists.
    /// </summary>
    public interface IValidateablePayload : IPayload
    {
        /// <summary>
        /// Perform payload validation.
        /// </summary>
        void Validate(List<string> errorsList);
    }
}
