#region Blobber!
// Blobber - Merges or embed referenced assemblies
// https://github.com/picrap/Blobber
// MIT License - http://opensource.org/licenses/MIT
#endregion

namespace TestApplication
{
    using System;
    using System.Linq;
    using System.Reflection;
    using EmbeddedLibrary;
    using MergedLibrary;

    class Program
    {
        static void D(MergedClass m)
        {
            m.G();
        }

        public class ReferencingMergedClass
        {
            public MergedClass Field;
            public MergedClass Property { get; set; }

            public string this[MergedClass c]
            {
                get { return c.ToString(); }
                set { }
            }
        }

        static void Main(string[] args)
        {
            var s = new MergedClass.SubType[0];
            var dt = typeof(MergedClass);
            var d = new MergedClass();
            d.G();
            var da = new[] { d };
            var dl = da.ToList();
            var c = new EmbeddedClass();
            c.F();
        }

        //private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        //{
        //}

        //private static Assembly OnTypeResolve(object sender, ResolveEventArgs args)
        //{
        //    return null;
        //}

        //private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        //{
        //    return null;
        //}
    }
}
