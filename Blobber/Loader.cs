#region Blobber!
// Blobber - Merges or embed referenced assemblies
// https://github.com/picrap/Blobber
// MIT License - http://opensource.org/licenses/MIT
#endregion

namespace Blobber
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Reflection;

    public static class Loader
    {
        public static void Setup()
        {
            var currentDomain = AppDomain.CurrentDomain;
            const string id = "Blobber:initialized";
            if (currentDomain.GetData(id) != null)
                return;
            currentDomain.SetData(id, new object());
            currentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        public static void Register()
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var argsName = args.Name;
            return Resolve(assembly, argsName);
        }

        public static Assembly Resolve(Assembly assembly, string assemblyName)
        {
            return GetEmbeddeddAssembly(assembly, assemblyName) ?? GetMergedAssembly(assembly, assemblyName);
        }

        /// <summary>
        /// Gets the embeddedd assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        private static Assembly GetEmbeddeddAssembly(Assembly assembly, string name)
        {
            var resourceStream = assembly.GetManifestResourceStream(GetEmbeddedAssemblyResourceName(name));
            if (resourceStream == null)
                return null;

            Trace.WriteLine($"Blobber: loading embedded assembly {name}");
            using (var assemblyStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(resourceStream, CompressionMode.Decompress))
                    gzipStream.CopyTo(assemblyStream);
                return Assembly.Load(assemblyStream.ToArray());
            }
        }

        internal static string GetEmbeddedAssemblyResourceName(string name) => $"blobber:embedded.gz:{name}";

        private static Assembly GetMergedAssembly(Assembly assembly, string name)
        {
            if (assembly.GetManifestResourceInfo(GetMergedAssemblyResourceName(name)) != null)
                return assembly;
            return null;
        }

        internal static string GetMergedAssemblyResourceName(string name) => $"blobber:merged:{name}";
    }
}
