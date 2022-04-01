using System;
using System.ComponentModel.DataAnnotations;

namespace Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads
{
    public class AnnotatedPayload
    {
        [Range(-5, 20)]
        public int SomeIntValue { get; set; }

        [MinLength(1)]
        [MaxLength(5)]
        public string SomeStringValue { get; set; }
    }
}
