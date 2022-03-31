using System;
using System.IO;
using System.Reflection;
using System.Threading;
using DotNetCore.CAP.Concurrency.SqlServer.Tests.Extensions;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;

namespace DotNetCore.CAP.Concurrency.SqlServer.Tests.Integration
{
    public abstract class IntegrationTestBase : IDisposable
    {
        private readonly FileInfo _dependenciesYamlFile;
        protected readonly ICompositeService DockerContainer;

        protected IntegrationTestBase(string dependenciesYamlFile, params WaitFor[] waitForConditions)
        {
            var manualResetEventSlim = new ManualResetEventSlim();

            FileInfo dependenciesFile = null;
            dependenciesYamlFile
                .GetTemporaryEmbeddedFileInfo(Assembly.GetExecutingAssembly())
                .ContinueWith(x =>
                {
                    dependenciesFile = x.Result;

                    manualResetEventSlim.Set();
                });
            manualResetEventSlim.Wait();

            _dependenciesYamlFile = dependenciesFile!;

            var builder = new Builder()
                .UseContainer()
                .UseCompose()
                .FromFile(_dependenciesYamlFile.FullName);

            foreach (var waitForCondition in waitForConditions)
            {
                switch (waitForCondition)
                {
                    case WaitForHttp http:
                        builder.WaitForHttp(http.Service, http.Url);
                        break;
                    case WaitForPort port:
                        builder.WaitForPort(port.Service, $"{port.Port}/{port.Protocol}");
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }

            DockerContainer = builder.Build().Start();
        }

        public void Dispose()
        {
            DockerContainer.Dispose();
            try
            {
                File.Delete(_dependenciesYamlFile.FullName);
            }
            catch
            {
                //intentionally empty
            }
        }
    }
}
