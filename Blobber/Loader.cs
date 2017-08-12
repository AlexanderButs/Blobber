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
    using System.Linq;
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

        public static void SetupResolver()
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var argsName = new AssemblyName(args.Name).ToString();
            return Resolve(assembly, argsName) ?? Resolve(args.RequestingAssembly, argsName);// ?? ResolveAll(args.Name);
        }

        /// <summary>
        /// Resolves assembly in all assemblies from current domain.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <returns></returns>
        private static Assembly ResolveAll(string assemblyName)
        {
            return AppDomain.CurrentDomain.GetAssemblies().Select(a => Resolve(a, assemblyName)).FirstOrDefault(a => a != null);
        }

        /// <summary>
        /// Tries to resolve an assembly given its name, by looking in the current assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <returns></returns>
        public static Assembly Resolve(Assembly assembly, string assemblyName)
        {
            if (assembly == null)
                return null;
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

        internal static string GetEmbeddedAssemblyResourceName(string name) => $"\u2302.gz:{name}";

        private static Assembly GetMergedAssembly(Assembly assembly, string name)
        {
            if (assembly.GetManifestResourceInfo(GetMergedAssemblyResourceName(name)) != null)
                return assembly;
            return null;
        }

        internal static string GetMergedAssemblyResourceName(string name) => $"\u2302:{name}";
    }
}
