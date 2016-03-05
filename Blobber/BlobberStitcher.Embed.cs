#region Blobber!
// Blobber - Merges or embed referenced assemblies
// https://github.com/picrap/Blobber
// MIT License - http://opensource.org/licenses/MIT
#endregion

namespace Blobber
{
    using System.IO;
    using System.IO.Compression;
    using dnlib.DotNet;
    using Microsoft.Build.Construction;
    using StitcherBoy.Project;

    partial class BlobberStitcher
    {
        private void Embed(ModuleDefMD2 moduleDef, AssemblyReference assemblyReference)
        {
            var gzippedAssembly = GetGZippedAssembly(assemblyReference);
            moduleDef.Resources.Add(new EmbeddedResource($"blobbed:{assemblyReference.AssemblyName}", gzippedAssembly));
        }

        private static byte[] GetGZippedAssembly(AssemblyReference assemblyReference)
        {
            using (var zippedStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(zippedStream, CompressionLevel.Optimal))
                using (var assemblyStream = File.OpenRead(assemblyReference.Path))
                {
                    assemblyStream.CopyTo(gzipStream);
                }
                return zippedStream.ToArray();
            }
        }
    }
}
