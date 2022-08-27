using System;

using Ant0nRocket.Lib.Dodb.DtoPayloads;

namespace Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads
{
    public class TestPayload : DtoPayloadBase
    {
        public Guid R { get; set; } = Guid.NewGuid();

        public DateTime DateT { get; set; } = DateTime.Now;
    }
}
