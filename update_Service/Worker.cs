using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace update_Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string targetFolder = @"C:\_git\grpc_updater\myservice\bin\Debug\net5.0\win-x64\publish";
            var updateLogic = new UpdateLogic(targetFolder);

            var minDelayMilliseconds = 5000;
            var maxDelayMilliseconds = 60000;
            var pow = 1.0;
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                try
                {
                    var channel = new Channel("127.0.0.1:30051", ChannelCredentials.Insecure);
                    var client  = new Registry.Registry.RegistryClient(channel);
                    await updateLogic.UpdateCycle(client, stoppingToken);
                    channel.ShutdownAsync().Wait();
                    pow = 1;
                    await Task.Delay(2000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception)
                {
                    int delay = (int)Math.Min(minDelayMilliseconds * pow, maxDelayMilliseconds);
                    await Task.Delay(delay);
                    pow *= 1.5;
                }
            }
        }
    }
}
