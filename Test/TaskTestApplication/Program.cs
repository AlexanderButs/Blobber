#region Blobber!
// Blobber - Merges or embed referenced assemblies
// https://github.com/picrap/Blobber
// MIT License - http://opensource.org/licenses/MIT
#endregion

namespace TaskTestApplication
{
    using System;
    using EmbeddedLibrary;
    using EmbeddedPortableLibrary;
    using MergedLibrary;

    class Program
    {
        static void Main(string[] args)
        {
            var c = new EmbeddedClass();
            c.F();
            var d = new MergedClass();
            d.G();
            var e = new EmbeddedPortableClass();
            Console.WriteLine(e.H());
            Console.ReadLine();
        }
    }
}
