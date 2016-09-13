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

    public class Program
    {
        public class GenericToBeMerged : MergedGenericClass<int>
        {
            public static void Nop() { }
        }

        public class ToBeMerged : MergedClass
        { }

        public class LocalGeneric<T>
        {
            public static void Nop() { }
        }

        public class Local
        { }

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

        static void MergeGenericArgument()
        {
            var t = typeof(LocalGeneric<MergedClass>);
            var z = new LocalGeneric<MergedClass>();
        }

        static void MergeGenericType()
        {
            var t = typeof(MergedGenericClass<Local>);
            var z = new MergedGenericClass<Local>();
        }

        static void CallGenericType()
        {
            MergedGenericClass<Local>.Nop();
            LocalGeneric<MergedClass>.Nop();
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
