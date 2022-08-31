using System;

using Ant0nRocket.Lib.Dodb.Dto;
using Ant0nRocket.Lib.Dodb.Gateway;
using Ant0nRocket.Lib.Dodb.Gateway.Abstractions;
using Ant0nRocket.Lib.Std20;
using Ant0nRocket.Lib.Std20.IO;
using Ant0nRocket.Lib.Std20.Logging;

namespace Ant0nRocket.Lib.Dodb.Tests
{
    /// <summary>
    /// Base class for testing libraries based on Dodb
    /// </summary>
    public abstract class DodbTestsBase
    {
        public DodbTestsBase()
        {
            Initialize();
        }

        private static bool _isInitialized = false;

        private static void Initialize()
        {
            if (_isInitialized) return;

            // Configuring required settings...
            Logger.LogToBasicLogWritter = true;
            BasicLogWritter.LogFileNamePrefix = "DodbGateway.Tests_";
            _ = FileSystemUtils.Delete(BasicLogWritter.LogDirectory);

            Logger.Log($"{nameof(Logger.LogToBasicLogWritter)} set to {Logger.LogToBasicLogWritter}");
            Logger.Log($"Log directory '{BasicLogWritter.LogDirectory}' clean-up");
            Logger.Log($"{nameof(BasicLogWritter.LogFileNamePrefix)} set to {BasicLogWritter.LogFileNamePrefix}");


            Ant0nRocketLibConfig.IsPortableMode = true;
            Logger.Log($"{nameof(Ant0nRocketLibConfig.IsPortableMode)} set to {Ant0nRocketLibConfig.IsPortableMode}");

            _isInitialized = true;
            Logger.Log("Tests initialization finished");
        }

        protected static DtoOf<T> CreateDto<T>(Guid? userId = default) where T : class, new() =>
            DodbGateway.CreateDto<T>(userId);

        protected IGatewayResponse Push(DtoBase dto) => DodbGateway.PushDto(dto);
    }
}
