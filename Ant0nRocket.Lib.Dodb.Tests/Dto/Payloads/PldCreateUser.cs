using Ant0nRocket.Lib.Dodb.Dto.Payloads;
using Ant0nRocket.Lib.Dodb.Tests.Model;

namespace Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads
{
    public class PldCreateUser : DtoPayloadBase
    {
        public User? Value { get; set; } = new();
    }
}
