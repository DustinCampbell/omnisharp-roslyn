using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OmniSharp.Utilities
{
    internal static class FileUtilities
    {
        public async static Task<MemoryStream> ReadFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var memoryStream = new MemoryStream();
            var buffer = new byte[1024];

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
            {
                int bytesRead;
                do
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    memoryStream.Write(buffer, 0, bytesRead);
                }
                while (bytesRead > 0);
            }

            memoryStream.Position = 0;

            return memoryStream;
        }
    }
}
