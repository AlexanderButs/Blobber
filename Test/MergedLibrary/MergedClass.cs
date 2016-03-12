#region Blobber!
// Blobber - Merges or embed referenced assemblies
// https://github.com/picrap/Blobber
// MIT License - http://opensource.org/licenses/MIT
#endregion

namespace MergedLibrary
{
    using System;

    public class MergedClass
    {
        public class SubType { }

        public void G()
        {
            Console.WriteLine("Salut depuis MergedClass");
        }
    }
}
