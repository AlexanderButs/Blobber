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
    using StitcherBoy.Project;

    partial class BlobberStitcher
    {
        private void Embed(ModuleDefMD2 targetModule, AssemblyReference assemblyReference, string assemblyReferencePath)
        {
            Logging.Write("Embedding {0} from {1}", (object)assemblyReference.Assembly.Name.String, assemblyReferencePath);
            var gzippedAssembly = GetGZippedAssembly(assemblyReferencePath);
            targetModule.Resources.Add(new EmbeddedResource(Loader.GetEmbeddedAssemblyResourceName(assemblyReference.Assembly.Name.ToString()), gzippedAssembly));
            File.Delete(assemblyReference.Path);
        }

        /// <summary>
        /// Gets the Gzipped assembly.
        /// </summary>
        /// <param name="assemblyReferencePath">The assembly reference path.</param>
        /// <returns></returns>
        private static byte[] GetGZippedAssembly(string assemblyReferencePath)
        {
            using (var zippedStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(zippedStream, CompressionLevel.Optimal))
                using (var assemblyStream = File.OpenRead(assemblyReferencePath))
                    assemblyStream.CopyTo(gzipStream);
                return zippedStream.ToArray();
            }
        }
    }
}
