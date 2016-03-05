#region Blobber!
// Blobber - Merges or embed referenced assemblies
// https://github.com/picrap/Blobber
// MIT License - http://opensource.org/licenses/MIT
#endregion

namespace TestApplication
{
    using EmbeddedLibrary;
    using MergedLibrary;

    class Program
    {
        static void Main(string[] args)
        {
            var c = new EmbeddedClass();
            c.F();
            var d = new MergedClass();
            d.G();
        }
    }
}
