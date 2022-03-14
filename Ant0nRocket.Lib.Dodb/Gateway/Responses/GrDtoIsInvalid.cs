using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Attributes;

namespace Ant0nRocket.Lib.Dodb.Gateway.Responses
{
    /// <summary>
    /// Returned when there are some errors during a validation.
    /// </summary>
    [IsSuccess(false)]
    public class GrDtoIsInvalid : GatewayResponse
    {
        public List<string> Errors { get; } = new();

        public GrDtoIsInvalid(List<string> errorsList) =>
            errorsList.ForEach(e => Errors.Add(e));

        public GrDtoIsInvalid(string errorMessage) =>
            Errors.Add(errorMessage);
    }
}
