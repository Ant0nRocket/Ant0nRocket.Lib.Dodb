using Ant0nRocket.Lib.Dodb.Tests.Contexts;
using Ant0nRocket.Lib.Std20.DependencyInjection;

namespace Ant0nRocket.Lib.Dodb.Tests.Services
{
    public abstract class ServiceBase
    {
        protected TestDbContext GetDbContext() => DI.Get<TestDbContext>();
    }
}
