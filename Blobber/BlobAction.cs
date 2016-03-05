#region Blobber!
// Blobber - Merges or embed referenced assemblies
// https://github.com/picrap/Blobber
// MIT License - http://opensource.org/licenses/MIT
#endregion

namespace Blobber
{
    public enum BlobAction
    {
        /// <summary>
        /// No action, keep reference as reference
        /// </summary>
        None,

        /// <summary>
        /// Merges the assembly
        /// </summary>
        Merge,

        /// <summary>
        /// Embeds the assembly as resource
        /// </summary>
        Embed,
    }
}
