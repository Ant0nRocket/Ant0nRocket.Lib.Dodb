using Ant0nRocket.Lib.Dodb.Dto.Payloads;
using Ant0nRocket.Lib.Dodb.Dto.Payloads.Abstractions;

namespace Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads.Mock
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
