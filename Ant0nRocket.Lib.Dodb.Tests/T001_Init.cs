using Ant0nRocket.Lib.Dodb.Tests.Contexts;
using Ant0nRocket.Lib.Std20.DependencyInjection;

using NUnit.Framework;

namespace Ant0nRocket.Lib.Dodb.Tests
{
    internal class T001_Init
    {
        [Test]
        public void T001_InitContext()
        {
            var context = DI.Get<TestDbContext>();
            Assert.IsNotNull(context);
        }
    }
}
