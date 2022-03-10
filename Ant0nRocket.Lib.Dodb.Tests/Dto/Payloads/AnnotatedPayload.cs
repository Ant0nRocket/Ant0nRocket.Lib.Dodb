using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Dtos;

using System;
using System.ComponentModel.DataAnnotations;

namespace Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads
{
    public class AnnotatedPayload
    {
        [Range(-5, 20)]
        public int SomeIntValue { get; set; }

        [MaxLength(5)]
        [MinLength(1)]
        public string SomeStringValue { get; set; }
    }
}
