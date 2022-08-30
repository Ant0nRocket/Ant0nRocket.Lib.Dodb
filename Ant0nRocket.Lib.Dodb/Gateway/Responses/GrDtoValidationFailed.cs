using Ant0nRocket.Lib.Dodb.Gateway.Abstractions;
using Ant0nRocket.Lib.Dodb.Gateway.Responses.Attributes;

namespace Ant0nRocket.Lib.Dodb.Gateway.Responses
{
    /// <summary>
    /// Returned when there are some errors during a validation.
    /// </summary>
    [IsSuccess(false)]
    public class GrDtoValidationFailed : IGatewayResponse
    {
        public List<string> Errors { get; } = new();

        public GrDtoValidationFailed(List<string> errorsList) =>
            errorsList.ForEach(e => Errors.Add(e));

        public GrDtoValidationFailed(string errorMessage) =>
            Errors.Add(errorMessage);
    }
}
