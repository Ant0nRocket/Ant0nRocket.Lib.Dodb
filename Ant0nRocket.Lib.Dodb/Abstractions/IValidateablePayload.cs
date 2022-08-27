namespace Ant0nRocket.Lib.Dodb.Abstractions
{
    /// <summary>
    /// Delegate for handling validation errors.
    /// </summary>
    public delegate void ValidationErrorHandler(string propertyName, string errorText);

    /// <summary>
    /// Payload that has <see cref="Validate(ValidationErrorHandler?)"/> inside.<br />
    /// Lib will call this method dynamically if it exists.
    /// </summary>
    public interface IValidateablePayload : IPayload
    {
        /// <summary>
        /// Perform payload validation. Action accepts
        /// </summary>
        void Validate(ValidationErrorHandler? onValidationErrorFound);
    }
}
