using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Registry;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace portal.Services
{

    public class RegistryImpl : Registry.Registry.RegistryBase
    {
        const string folder         = @"C:\_test";
        const int fileChunkSize = 1024*16;
        
        public override async Task Download(Definition request, IServerStreamWriter<Fragment> responseStream, ServerCallContext context)
        {
            using var fs = File.Open(Path.Combine(folder, request.Filename), FileMode.Open);
            int bytesRead;
            var buffer = new byte[fileChunkSize];
            while ((bytesRead = await fs.ReadAsync(buffer)) > 0)
                await responseStream.WriteAsync(new Fragment { Chunk = ByteString.CopyFrom(buffer[0..bytesRead]) });
        }

        public override Task<Definitions> GetLatest(Empty request, ServerCallContext context)
        {
            var list = Directory.GetFiles(folder)
                .Select(f => new Definition { Filename = Path.GetFileName(f), Version = GetInfo(f)})
                .Where(def => def.Version != null)
                ;
            var ret = new Definitions();
            ret.List.AddRange(list);
            return Task.FromResult(ret);
        }

        public string GetInfo(string filename) => FileVersionInfo.GetVersionInfo(filename).FileVersion;
    }
}
