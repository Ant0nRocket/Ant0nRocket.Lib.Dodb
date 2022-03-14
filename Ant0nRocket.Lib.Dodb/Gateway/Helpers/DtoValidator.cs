using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Std20.Logging;

using System.ComponentModel.DataAnnotations;

namespace Ant0nRocket.Lib.Dodb.Gateway.Helpers
{
    internal class DtoValidator<T> where T : class, new()
    {
        private readonly DtoOf<T> dto;

        public List<string> ErrorsList { get; private set; }

        public DtoValidator(DtoOf<T> dto)
        {
            this.dto = dto;
        }

        public DtoValidator<T> Validate()
        {
            ErrorsList ??= new();
            ErrorsList.Clear();

            // Check basic properties
            if (dto.Id == Guid.Empty) ErrorsList.Add($"{nameof(dto.Id)} is not set");

            if (DodbConfig.ValidateAuthorId)
                if (dto.AuthorId == Guid.Empty) ErrorsList.Add($"{nameof(dto.AuthorId)} is not set");

            // Check payload using annotations
            var validationContext = new ValidationContext(dto.Payload);
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(dto.Payload, validationContext, validationResults, validateAllProperties: true))
                validationResults.ForEach(vr => ErrorsList.Add(vr.ErrorMessage));

            return this;
        }

        public bool DidntFindAnyErrors => ErrorsList?.Count == 0;

        public bool HasFoundErrors => !DidntFindAnyErrors;
    }
}
