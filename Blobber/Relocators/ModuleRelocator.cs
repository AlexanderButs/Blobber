#region Blobber!
// Blobber - Merges or embed referenced assemblies
// https://github.com/picrap/Blobber
// MIT License - http://opensource.org/licenses/MIT
#endregion

namespace Blobber.Relocators
{
    using dnlib.DotNet;

    internal class ModuleRelocator : Relocator
    {
        private readonly ModuleDefMD2 _oldModule;
        private readonly ModuleDefMD2 _newModule;

        public ModuleRelocator(ModuleDefMD2 oldModule, ModuleDefMD2 newModule)
            : base(newModule)
        {
            _oldModule = oldModule;
            _newModule = newModule;
        }

        protected override TypeSig TryRelocateTypeRef(TypeRef typeRef)
        {
            if (typeRef == null)
                return null;
            var scope = typeRef?.Scope as IFullName;
            if (scope?.FullName != _oldModule.Assembly.FullName)
                return null;
            var typeDef = _newModule.Find(typeRef.FullName, false);
            return typeDef?.ToTypeSig();
            //return new TypeRefUser(null, typeDef.Namespace, typeDef.Name, _newModule);
        }
    }
}
