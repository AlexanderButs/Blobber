﻿#region Blobber!
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
    using StitcherBoy.Project;

    partial class BlobberStitcher
    {
        private void Merge(ModuleDefMD2 targetModule, AssemblyReference assemblyReference)
        {
            Logging.Write("Merging   {0}", assemblyReference.AssemblyName);

            var referenceModule = ModuleDefMD.Load(assemblyReference.Path);
            {
                targetModule.Resources.Add(new EmbeddedResource(Loader.GetMergedAssemblyResourceName(assemblyReference.AssemblyName.ToString()), new byte[0]));

                var allReferenceTypes = referenceModule.Types.ToArray();
                referenceModule.Types.Clear();
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
                                referenceCCtor.Name = referenceCCtor.Name + "/" + assemblyReference.Name;
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
                        var existingType = targetModule.Find(referenceType.FullName, true);
                        if (existingType != null)
                            targetModule.Types.Remove(existingType);
                        targetModule.Types.Add(referenceType);
                    }
                }

                // TODO: resources
                // TODO: attributes?
            }
//            File.Delete(assemblyReference.Path);
        }
    }
}
