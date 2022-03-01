using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Dodb.Dtos.Payloads.UsersService;
using Ant0nRocket.Lib.Dodb.Tests.Contexts;
using Ant0nRocket.Lib.Std20.DependencyInjection;

using static Ant0nRocket.Lib.Dodb.Services.Services;

using NUnit.Framework;
using Ant0nRocket.Lib.Dodb.Abstractions;

namespace Ant0nRocket.Lib.Dodb.Tests
{
    internal class T001_Init
    {
        [Test]
        public void T001_Playing_Games()
        {
            var us = GetService<IDodbDocumentsService>();

        }
    }
}
