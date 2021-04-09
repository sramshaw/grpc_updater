using System;
using Grpc.Core;
using System.Collections.Generic;
using System.Threading;

namespace updater
{
    class Program
    {
        private const string targetFolder = "c:/etc/service/";

        public static void Main(string[] args)
        {
            var channel = new Channel("127.0.0.1:30051", ChannelCredentials.Insecure);

            var client = new Registry.Registry.RegistryClient(channel);
            var worker = new UpdateLogic(targetFolder);

            worker.UpdateCycle(client);
            channel.ShutdownAsync().Wait();

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
