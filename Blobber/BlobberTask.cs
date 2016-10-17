#region Blobber!
// Blobber - Merges or embed referenced assemblies
// https://github.com/picrap/Blobber
// MIT License - http://opensource.org/licenses/MIT
#endregion

using Blobber;

// ReSharper disable once CheckNamespace
public class BlobberTask : StitcherTask<BlobberStitcher>
{
    /// <summary>
    /// Entry point for nested process (for isolation).
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns></returns>
    public static int Main(string[] args) => Run(new BlobberTask(), args);
}
