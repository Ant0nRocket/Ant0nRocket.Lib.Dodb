using System.ComponentModel.DataAnnotations;

using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Std20.Extensions;

namespace Ant0nRocket.Lib.Dodb.Gateway.Helpers
{
    internal class DtoValidator
    {
        private readonly Dto dto;

        public List<string> ErrorsList { get; private set; }

        public DtoValidator(Dto dto)
        {
            this.dto = dto;
        }

        public DtoValidator Validate()
        {
            ErrorsList ??= new();
            ErrorsList.Clear();

            // Check basic properties
            if (dto.Id == Guid.Empty) 
                ErrorsList.Add($"{nameof(dto.Id)} is not set");

            // Check payload using annotations
            var dtoPayload = dto.GetPropertyValue("Payload");
            var validationContext = new ValidationContext(dtoPayload);
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(dtoPayload, validationContext, validationResults, validateAllProperties: true))
                validationResults.ForEach(vr => ErrorsList.Add(vr.ErrorMessage));

            if (dtoPayload is IValidateablePayload validateablePayload)
                validateablePayload.Validate(ErrorsList);

            return this;
        }

        public bool DidntFindAnyErrors => ErrorsList?.Count == 0;

        public bool HasFoundErrors => !DidntFindAnyErrors;
    }
}
