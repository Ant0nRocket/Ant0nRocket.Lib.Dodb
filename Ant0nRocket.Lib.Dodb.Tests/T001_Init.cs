
using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Dodb.Gateway;
using Ant0nRocket.Lib.Dodb.Tests.Contexts;
using Ant0nRocket.Lib.Dodb.Tests.Services;

using NUnit.Framework;

using System;

namespace Ant0nRocket.Lib.Dodb.Tests
{
    public class TestPayload
    {
        public Guid R { get; set; } = Guid.NewGuid();
        public DateTime DateT { get; set; } = DateTime.Now;
    }

    public class T001_Init
    {
        [Test]
        public void T001_RegisterGetterAndHandlers()
        {
            DodbGateway.RegisterContextGetter(new Func<IDodbContext>(() => new TestDbContext()));
            DodbGateway.RegisterDtoHandler<Dto<TestPayload>>(obj => TestService.TestMethod(obj));
        }
    }
}
