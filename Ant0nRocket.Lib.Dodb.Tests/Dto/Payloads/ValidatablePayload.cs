using System.Collections.Generic;

using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.DtoPayloads;

namespace Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads
{
    internal class ValidatablePayload : DtoPayloadBase, IValidateablePayload
    {
        public int TestValue { get; set; } = 10;

        public void Validate(List<string> errorsList)
        {
            if (TestValue != 10)
                errorsList.Add("TestValue should be 10");
        }
    }
}
