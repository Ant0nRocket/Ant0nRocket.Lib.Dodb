using Ant0nRocket.Lib.Dodb.Abstractions;

namespace Ant0nRocket.Lib.Dodb.Gateway.Responces
{
    /// <summary>
    /// Returned when there are some errors during a validation.
    /// </summary>
    public class GrDtoIsInvalid : GatewayResponse
    {
        public List<string> Errors { get; } = new();

        public GrDtoIsInvalid(List<string> errorsList) =>
            errorsList.ForEach(e => Errors.Add(e));

        public GrDtoIsInvalid(string errorMessage) =>
            Errors.Add(errorMessage);
    }
}
