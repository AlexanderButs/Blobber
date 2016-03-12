#region Blobber!
// Blobber - Merges or embed referenced assemblies
// https://github.com/picrap/Blobber
// MIT License - http://opensource.org/licenses/MIT
#endregion

namespace Blobber.Utility
{
    using dnlib.DotNet;

    /// <summary>
    /// Extensions to dnlib
    /// </summary>
    internal static class dnlibExtensions
    {
        /// <summary>
        /// Indicates whether the given <see cref="TypeSig" /> belongs to given module
        /// </summary>
        /// <param name="typeSig">The type sig.</param>
        /// <param name="module">The module.</param>
        /// <returns></returns>
        public static bool BelongsTo(this TypeSig typeSig, ModuleDefMD2 module) => BelongsTo(typeSig.Scope as AssemblyRef, module);

        /// <summary>
        /// Indicates whether the given <see cref="TypeSig" /> belongs to given module
        /// </summary>
        /// <param name="typeDefOrRef">The type definition or reference.</param>
        /// <param name="module">The module.</param>
        /// <returns></returns>
        public static bool BelongsTo(this ITypeDefOrRef typeDefOrRef, ModuleDefMD2 module) => BelongsTo(typeDefOrRef.Scope as AssemblyRef, module);

        /// <summary>
        /// Indicates whether the given <see cref="AssemblyRef"/> belongs to given module
        /// </summary>
        /// <param name="assemblyScope">The assembly scope.</param>
        /// <param name="module">The module.</param>
        /// <returns></returns>
        public static bool BelongsTo(this AssemblyRef assemblyScope, ModuleDefMD2 module)
        {
            return assemblyScope?.FullName == module.Assembly.FullName;
        }
    }
}
