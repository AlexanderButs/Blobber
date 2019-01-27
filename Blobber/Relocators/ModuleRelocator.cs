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

        protected override ITypeDefOrRef TryRelocateTypeRef(TypeRef typeRef)
        {
            if (typeRef == null)
                return null;
            var scope = typeRef.Scope as IFullName;
            if (scope?.FullName != _oldModule.Assembly.FullName)
                return null;
            var typeDef = _newModule.Find(typeRef.FullName, false);
            // this seems to be useless: TODO: check and remove
            if (typeDef == null)
                typeDef = _newModule.Find(BlobberStitcher.GetMergedName(typeRef, _oldModule), false);
            return typeDef;
        }

        protected override ITypeDefOrRef TryRelocateTypeDef(TypeDef typeDef)
        {
            if (typeDef == null)
                return null;
            var scope = typeDef.Scope as IFullName;
            if (scope?.FullName != _oldModule.Assembly.FullName)
                return null;
            var relocatedTypeDef = _newModule.Find(typeDef.FullName, false);
            // this seems to be useless: TODO: check and remove
            if (relocatedTypeDef == null)
                relocatedTypeDef = _newModule.Find(BlobberStitcher.GetMergedName(typeDef, _oldModule), false);
            return relocatedTypeDef;
        }
    }
}
