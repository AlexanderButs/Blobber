#region Blobber!
// Blobber - Merges or embed referenced assemblies
// https://github.com/picrap/Blobber
// MIT License - http://opensource.org/licenses/MIT
#endregion

namespace Blobber.Relocators
{
    using dnlib.DotNet;

    public class ModuleRelocator : IRelocator
    {
        private readonly ModuleDefMD2 _oldModule;
        private readonly ModuleDefMD2 _newModule;

        public ModuleRelocator(ModuleDefMD2 oldModule, ModuleDefMD2 newModule)
        {
            _oldModule = oldModule;
            _newModule = newModule;
        }

        public TypeDef Relocate(IType type)
        {
            var scope = type?.Scope as IFullName;
            if (scope?.FullName != _oldModule.Assembly.FullName)
                return null;
            var typeDef = _newModule.Find(type.FullName, false);
            return typeDef;
        }
    }
}
