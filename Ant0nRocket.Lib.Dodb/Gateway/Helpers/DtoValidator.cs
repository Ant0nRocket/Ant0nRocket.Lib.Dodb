using System.ComponentModel.DataAnnotations;

using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Std20.Extensions;

namespace Ant0nRocket.Lib.Dodb.Gateway.Helpers
{
    internal class DtoValidator
    {
        private readonly Dto dto;

        public List<ValidationResult> ValidationResults { get; } = new();

        public List<string> ErrorsList => ValidationResults
            .Select(r => r.ErrorMessage ?? string.Empty)
            .Where(r => r != string.Empty)
            .Distinct()
            .ToList();

        public DtoValidator(Dto dto)
        {
            this.dto = dto;
        }

        public DtoValidator Validate()
        {
            ValidationResults.Clear();

            // Check basic properties
            if (dto.Id == Guid.Empty)
                ValidationResults.Add(new ValidationResult("Id is default"));

            if (dto.UserId == Guid.Empty)
                ValidationResults.Add(new ValidationResult("UserId is default"));

            // Check payload using annotations
            var dtoPayload = dto.GetPropertyValue("Payload");
            ValidateWithContext(dtoPayload);

            // If implements IValidatablePayload then validate with delegate
            if (dtoPayload is IValidateablePayload validateablePayload)
                validateablePayload.Validate(OnValidationErrorFound);

            // Some payloads have property Value, check it
            try
            {
                var payloadValue = dtoPayload.GetPropertyValue("Value");
                if (payloadValue != null)
                    ValidateWithContext(payloadValue);
            }
            catch
            {
                // nothing to handle, instance could be without Value
            }

            return this;
        }

        private void ValidateWithContext(object obj)
        {
            var validationContext = new ValidationContext(obj);
            Validator.TryValidateObject(obj, validationContext, ValidationResults, validateAllProperties: true);
        }

        private void OnValidationErrorFound(string propertyName, string errorText)
        {
            ValidationResults.Add(new ValidationResult(errorText, new[] { propertyName }));
        }


        [Obsolete]
        public bool DidntFindAnyErrors => ErrorsList?.Count == 0;

        [Obsolete]
        public bool HasFoundErrors => !DidntFindAnyErrors;

    }
}
