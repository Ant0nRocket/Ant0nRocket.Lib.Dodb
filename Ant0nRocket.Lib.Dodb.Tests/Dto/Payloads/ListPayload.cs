using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Ant0nRocket.Lib.Dodb.DataAnnotation;

namespace Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads
{
    internal class ListPayload
    {
        [MinLength(1)]
        [ValidateEachElement]
        public List<AnnotatedPayload> Items { get; set; } = new();
    }
}
