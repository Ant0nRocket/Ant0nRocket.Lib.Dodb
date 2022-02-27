using Ant0nRocket.Lib.Dodb.Dto;
using Ant0nRocket.Lib.Dodb.Services.Responses;
using Ant0nRocket.Lib.Dodb.Services.Responses.DocumentsService;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ant0nRocket.Lib.Dodb.Services
{
    public static class DocumentsService
    {
        public static ResponseBase Push<T>(DtoBase<T> dto) where T : class, new()
        {
            return new R_DocumentCreated();
        }
    }
}
