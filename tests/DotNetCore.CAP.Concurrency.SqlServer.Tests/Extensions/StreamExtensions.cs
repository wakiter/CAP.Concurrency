using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Concurrency.SqlServer.Tests.Extensions
{
    internal static class StreamExtensions
    {
        public static async Task<byte[]> GetEmbeddedFileBytes(this string filename, Assembly assembly)
        {
            var fullFilename = assembly
                .GetManifestResourceNames()
                .Single(str => str.EndsWith(filename, StringComparison.InvariantCultureIgnoreCase));

            await using var stream = assembly.GetManifestResourceStream(fullFilename);
            using var reader = new StreamReader(stream!);
            await using var memReader = new MemoryStream();
            await reader.BaseStream.CopyToAsync(memReader);
            return memReader.ToArray();
        }

        public static async Task<FileInfo> GetTemporaryEmbeddedFileInfo(this string filename, Assembly assembly)
        {
            var extension = Path.GetExtension(filename);
            var tempFilename = GetTempFilePathWithExtension(extension);
            var fileBytes = await filename.GetEmbeddedFileBytes(assembly);
            await File.WriteAllBytesAsync(tempFilename, fileBytes);

            return new FileInfo(tempFilename);
        }

        private static string GetTempFilePathWithExtension(string extension)
        {
            var path = Path.GetTempPath();
            var fileName = Path.ChangeExtension(Guid.NewGuid().ToString(), extension);
            return Path.Combine(path, fileName);
        }
    }
}
