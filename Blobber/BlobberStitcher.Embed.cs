﻿#region Blobber!
// Blobber - Merges or embed referenced assemblies
// https://github.com/picrap/Blobber
// MIT License - http://opensource.org/licenses/MIT
#endregion

namespace Blobber
{
    using System.IO;
    using System.IO.Compression;
    using dnlib.DotNet;

    partial class BlobberStitcher
    {
        private void Embed(ModuleDefMD2 targetModule, AssemblyFile assemblyFile)
        {
            Logging.Write("Embedding {0} from {1}", assemblyFile.Reference.Module.Name.String, assemblyFile.Path);
            var gzippedAssembly = GetGZippedAssembly(assemblyFile.Path);
            targetModule.Resources.Add(new EmbeddedResource(Loader.GetEmbeddedAssemblyResourceName(assemblyFile.Reference.Module.Name.String), gzippedAssembly));
            assemblyFile.DeleteIfLocal();
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
