using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads;
using Ant0nRocket.Lib.Dodb.Tests.Model;
using Ant0nRocket.Lib.Std20.Logging;

using NUnit.Framework;

namespace Ant0nRocket.Lib.Dodb.Tests
{
    public class T700_Lab : DodbTestsBase
    {
        [Test]
        public void T000_Init()
        {
            new T000_Prepare().T001_RegisterGetterAndHandlers();
        }


        [Test]
        public void T001_L1()
        {
            var dto = CreateDto<PldCreateUser>();
            dto.Payload.Name = "Dodb";
            dto.Payload.PasswordHash = "1";
            Logger.Log("================================");
            _ = Push(dto);
        }
    }
}
