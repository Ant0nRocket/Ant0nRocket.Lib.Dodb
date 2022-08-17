using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Entities;

namespace Ant0nRocket.Lib.Dodb.DtoPayloads
{
    public class PldCreateUser : User, IValidateablePayload
    {
        public void Validate(List<string> errorsList)
        {

        }
    }
}
