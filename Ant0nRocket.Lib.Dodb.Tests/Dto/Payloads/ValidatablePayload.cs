using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.DtoPayloads;

namespace Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads
{
    internal class ValidatablePayload : DtoPayloadBase, IValidateablePayload
    {
        public int TestValue { get; set; } = 10;

        public void Validate(ValidationErrorHandler? onValidationErrorFound)
        {
            if (onValidationErrorFound == null) return;

            if (TestValue != 10)
                onValidationErrorFound(nameof(TestValue), "Must be 10");
        }
    }
}
