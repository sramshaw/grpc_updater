using System;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.IO;
using Google.Protobuf.WellKnownTypes;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Grpc.Core;
using System.Threading;

namespace updater
{
    public class UpdateLogic
    {
        private string targetFolder;

        public UpdateLogic(string targetFolder)
        {
            this.targetFolder = targetFolder;
        }

        private void StartService(string serviceName, int timeoutMilliseconds)
        {
            ServiceController service = new ServiceController(serviceName);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        private void StopService(string serviceName, int timeoutMilliseconds)
        {
            ServiceController service = new ServiceController(serviceName);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task GetFile(Registry.Registry.RegistryClient client, Registry.Definition def)
        {
            var first = def;
            var needed = first.Filename;
            await using Stream fs = File.OpenWrite(needed + ".tmp");
            using var call = client.Download(first);
            try
            {
                await foreach (Registry.Fragment chunkMsg in call.ResponseStream.ReadAllAsync())
                    await fs.WriteAsync(chunkMsg.Chunk.ToByteArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void UpdateCycle(Registry.Registry.RegistryClient client)
        {
            var reply = client.GetLatest(new Empty { }).List
                .OrderBy(def => def.Filename);
            Console.WriteLine("Definitions:\n" + string.Join("\n", reply.Select(u => u.Filename)));
            var targetFiles = Directory.GetFiles(targetFolder)
                .Select(name => new Registry.Definition { Filename = Path.GetFileName(name), Version = FileVersionInfo.GetVersionInfo(name).FileVersion })
                .OrderBy(def => def.Filename);

            var changedNeeded = reply.Where(def => !targetFiles.Any(v => v.Filename == def.Filename)
                                                || targetFiles.Any(v => v.Filename == def.Filename && v.Version != def.Version))
                .ToList();

            if (changedNeeded.Any())
            {
                foreach (var chg in changedNeeded)
                {
                    Console.WriteLine("Working on file: " + chg.Filename);
                    GetFile(client, chg).Wait();
                }
                StopService("myservice", 1000);
                foreach (var chg in changedNeeded)
                {
                    Console.WriteLine("Moving file: " + chg.Filename);
                    File.Move(chg.Filename + ".tmp", targetFolder + chg.Filename, true);
                }
                StartService("myservice", 1000);
            }
            else
            {
                Console.WriteLine("No changes detected");
            }
        }
    }

    public static class Extension
    {
        public async static IAsyncEnumerable<T> ReadAllAsync<T>(this IAsyncStreamReader<T> streamReader, CancellationToken cancellationToken = default)
        {
            if (streamReader == null)
                throw new ArgumentNullException(nameof(streamReader));

            while (await streamReader.MoveNext(cancellationToken))
                yield return streamReader.Current;
        }
    }
}