
using System;

namespace Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads
{
    public class TestPayload
    {
        public Guid R { get; set; } = Guid.NewGuid();

        public DateTime DateT { get; set; } = DateTime.Now;
    }
}
