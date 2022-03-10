
using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Dtos;

using System;

namespace Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads
{
    public class TestPayload
    {
        public Guid R { get; set; } = Guid.NewGuid();

        public DateTime DateT { get; set; } = DateTime.Now;
    }
}
