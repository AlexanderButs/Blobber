#region Blobber!
// Blobber - Merges or embed referenced assemblies
// https://github.com/picrap/Blobber
// MIT License - http://opensource.org/licenses/MIT
#endregion

using System.Reflection;
using Blobber;

public class BlobberTask : StitcherTask<BlobberStitcher>
{
    public static int Main(string[] args) => Run(new BlobberTask(), args);
}
