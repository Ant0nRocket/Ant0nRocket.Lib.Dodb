using System;
using System.ComponentModel.DataAnnotations;

using Ant0nRocket.Lib.Dodb.DtoPayloads;

namespace Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads
{
    public class AnnotatedPayload : DtoPayloadBase
    {
        [Range(-5, 40)]
        public int SomeIntValue { get; set; }

        [MinLength(1)]
        [MaxLength(5)]
        public string? SomeStringValue { get; set; }
    }
}
