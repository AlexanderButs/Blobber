#region Blobber!
// Blobber - Merges or embed referenced assemblies
// https://github.com/picrap/Blobber
// MIT License - http://opensource.org/licenses/MIT
#endregion

namespace Blobber
{
    using System;
    using System.IO;
    using System.Linq;
    using dnlib.DotNet;
    using dnlib.DotNet.Emit;
    using Relocators;
    using StitcherBoy.Reflection;

    partial class BlobberStitcher
    {
        /// <summary>
        /// Merges the specified target module.
        /// </summary>
        /// <param name="targetModule">The target module.</param>
        /// <param name="assemblyFile">The assembly file.</param>
        private IDisposable Merge(ModuleDefMD2 targetModule, AssemblyFile assemblyFile)
        {
            var moduleManager = new ModuleManager(assemblyFile.Path, false, null);
            {
                var baseName = GetBaseName(moduleManager.Module);
                Logging.Write("Merging   {0}", baseName);
                targetModule.Resources.Add(new EmbeddedResource(Loader.GetMergedAssemblyResourceName(baseName), new byte[0]));

                var allReferenceTypes = moduleManager.Module.Types.ToArray();
                moduleManager.Module.Types.Clear();
                foreach (var referenceType in allReferenceTypes)
                {
                    // <Module> is handled differently
                    if (referenceType.Name == "<Module>")
                    {
                        var referenceCCtor = referenceType.FindStaticConstructor();
                        // if there is a cctor in ref
                        if (referenceCCtor != null)
                        {
                            // if no target <Module> (I don't think this is even possible), then the current <Module> is copied
                            var targetModuleModuleType = targetModule.Find("<Module>", true);
                            if (targetModuleModuleType == null)
                            {
                                targetModule.Types.Add(referenceType);
                            }
                            else
                            {
                                // otherwise the ref cctor is renamed, inserted as a simple method and called
                                var targetModuleCctor = targetModuleModuleType.FindOrCreateStaticConstructor();
                                // 1. renaming
                                referenceCCtor.Name = referenceCCtor.Name + "/" + moduleManager.Module.Name;
                                referenceCCtor.Attributes &= ~MethodAttributes.SpecialName;
                                // 2. adding
                                targetModuleModuleType.Methods.Add(referenceCCtor);
                                // 3. calling
                                targetModuleCctor.Body.Instructions.Add(new Instruction(OpCodes.Call, targetModule.Import(referenceCCtor)));
                            }
                        }
                    }
                    else
                    {
                        // other case: simply move the type

                        // check if there is a conflict, and if there is, change new type name
                        var existingType = targetModule.Find(referenceType.FullName, true);
                        if (existingType != null)
                            referenceType.Name = GetMergedName(referenceType, moduleManager.Module);
                        // and add the type
                        targetModule.Types.Add(referenceType);
                    }
                }

                // TODO: resources
                // TODO: attributes?

                var relocator = new ModuleRelocator(moduleManager.Module, targetModule);
                relocator.Relocate();
            }
            assemblyFile.DeleteIfLocal();
            return moduleManager;
        }

        public static string GetMergedName(IType type, ModuleDef module)
        {
            return FullNameCreator.FullName(type, false, null, null) + $"@{module.Name}";
        }
    }
}
