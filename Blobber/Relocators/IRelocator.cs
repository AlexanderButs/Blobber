#region Blobber!
// Blobber - Merges or embed referenced assemblies
// https://github.com/picrap/Blobber
// MIT License - http://opensource.org/licenses/MIT
#endregion

namespace Blobber.Relocators
{
    using dnlib.DotNet;

    internal interface IRelocator
    {
        /// <summary>
        /// Relocates the specified type definition or reference.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        TypeDef Relocate(IType type);
    }
}
