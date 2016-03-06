#region Blobber!
// Blobber - Merges or embed referenced assemblies
// https://github.com/picrap/Blobber
// MIT License - http://opensource.org/licenses/MIT
#endregion

namespace TestApplication
{
    using System;
    using System.Reflection;
    using EmbeddedLibrary;
    using MergedLibrary;

    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            AppDomain.CurrentDomain.TypeResolve += OnTypeResolve;
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
            var c = new EmbeddedClass();
            c.F();
            var d = new MergedClass();
            d.G();
        }

        private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
        }

        private static Assembly OnTypeResolve(object sender, ResolveEventArgs args)
        {
            return null;
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            return null;
        }
    }
}
