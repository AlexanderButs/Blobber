#region Blobber!
// Blobber - Merges or embed referenced assemblies
// https://github.com/picrap/Blobber
// MIT License - http://opensource.org/licenses/MIT
#endregion

namespace Blobber
{
    using System.IO;
    using StitcherBoy.Project;

    public class AssemblyFile
    {
        /// <summary>
        /// Gets the reference.
        /// </summary>
        /// <value>
        /// The reference.
        /// </value>
        public AssemblyReference Reference { get; }

        /// <summary>
        /// Gets the path.
        /// </summary>
        /// <value>
        /// The path.
        /// </value>
        public string Path { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is local.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is local; otherwise, <c>false</c>.
        /// </value>
        public bool IsLocal { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyFile" /> class.
        /// </summary>
        /// <param name="reference">The reference.</param>
        /// <param name="targetModulePath">The target module path.</param>
        public AssemblyFile(AssemblyReference reference, string targetModulePath)
        {
            Reference = reference;
            var targetModuleDirectory = System.IO.Path.GetDirectoryName(targetModulePath);
            var assemblyName = System.IO.Path.GetFileName(reference.Path);
            var assemblyPath = System.IO.Path.Combine(targetModuleDirectory, assemblyName);
            IsLocal = File.Exists(assemblyPath);// && assemblyPath != reference.Path;
            if (IsLocal)
                Path = assemblyPath;
            else
                Path = reference.Path;
        }

        public void DeleteIfLocal()
        {
            if (IsLocal)
                File.Delete(Path);
        }
    }
}
