using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Ant0nRocket.Lib.Dodb.Dto.Payloads;
using Ant0nRocket.Lib.Dodb.Dto.Payloads.DataAnnotations;

namespace Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads.Mock
{
    public class ListPayload : DtoPayloadBase
    {
        [MinLength(1)]
        [ValidateEachElement]
        public List<AnnotatedPayload> Items { get; set; } = new();
    }
}
