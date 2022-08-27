using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Ant0nRocket.Lib.Dodb.DataAnnotation;
using Ant0nRocket.Lib.Dodb.DtoPayloads;

namespace Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads
{
    internal class ListPayload : DtoPayloadBase
    {
        [MinLength(1)]
        [ValidateEachElement]
        public List<AnnotatedPayload> Items { get; set; } = new();
    }
}
