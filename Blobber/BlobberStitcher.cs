#region Blobber!
// Blobber - Merges or embed referenced assemblies
// https://github.com/picrap/Blobber
// MIT License - http://opensource.org/licenses/MIT
#endregion

namespace Blobber
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using dnlib.DotNet;
    using dnlib.DotNet.Emit;
    using StitcherBoy.Weaving.Build;

    public partial class BlobberStitcher : AssemblyStitcher
    {
        protected override bool Process(AssemblyStitcherContext context)
        {
            try
            {
                Logging.Write("Assembly at {0}", context.AssemblyPath);
                bool processed = false;
                var directives = LoadDirectives();
                var disposables = new List<IDisposable>();
                ModuleWritten += delegate
                {
                    foreach (var disposable in disposables)
                        disposable.Dispose();
                };
                foreach (var reference in context.Dependencies)
                {
                    var assemblyFile = new AssemblyFile(reference, context.AssemblyPath);
                    var action = GetAction(assemblyFile, directives, context.Configuration);
                    switch (action)
                    {
                        case BlobAction.Embed:
                            Embed(context.Module, assemblyFile);
                            processed = true;
                            break;
                        case BlobAction.Merge:
                            disposables.Add(Merge(context.Module, assemblyFile));
                            processed = true;
                            break;
                        case null:
                            if (context.Configuration == "Debug")
                                Logging.Write("Assembly {0} ({1}) excluded from process (no matching action at all)", GetReferenceName(reference),
                                    reference.IsPrivate ? "private" : "non-private");
                            break;
                        case BlobAction.None:
                            if (context.Configuration == "Debug")
                                Logging.Write("Assembly {0} ({1}) excluded from process", GetReferenceName(reference), reference.IsPrivate ? "private" : "non-private");
                            break;
                    }
                }

                if (processed)
                    EmbedLoader(context.Module, context.TaskAssemblyPath);
                return processed;
            }
            catch
            {
            }
            return false;
        }

        private static string GetReferenceName(AssemblyDependency reference)
        {
            if (reference == null)
                return "(null)";
            return Path.GetFileNameWithoutExtension(reference.Path);
        }

        /// <summary>
        /// Embeds the loader.
        /// </summary>
        /// <param name="moduleDef">The module definition.</param>
        /// <param name="taskAssemblyPath">The task assembly path.</param>
        private void EmbedLoader(ModuleDefMD2 moduleDef, string taskAssemblyPath)
        {
            var assemblyLoaderTypeName = typeof(Loader).FullName;
            // import Loader type from this assembly
            var thisModuleDef = ModuleDefMD.Load(taskAssemblyPath);
            var loaderType = thisModuleDef.Find(assemblyLoaderTypeName, true);
            thisModuleDef.Types.Remove(loaderType);
            loaderType.Name = "\u2302";
            loaderType.Namespace = null;
            moduleDef.Types.Add(loaderType);
            // ensure it is called from module cctor
            var moduleType = moduleDef.Find("<Module>", true);
            var cctor = moduleType.FindOrCreateStaticConstructor();
            var loaderInitializeMethod = loaderType.FindMethod(nameof(Loader.Setup));
            cctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, moduleDef.Import(loaderInitializeMethod)));
        }

        /// <summary>
        /// Gets the action.
        /// </summary>
        /// <param name="assemblyReference">The assembly reference.</param>
        /// <param name="directives">The directives.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns></returns>
        private static BlobAction? GetAction(AssemblyFile assemblyReference, IList<BlobDirective> directives, string configuration)
        {
            BlobAction? action = null;
            foreach (var directive in directives)
            {
                var directiveAction = directive.Matches(assemblyReference.Reference, configuration);
                if (directiveAction.HasValue)
                    action = directiveAction.Value;
            }
            return action;
        }

        private static readonly Regex DirectiveEx = new Regex(@"^\s*(\((?<Configuration>([^#\)]+))\))?\s*(?<Scope>(\+|\-))?\s*(?<Assembly>[^\:]+)\s*\:\s*(?<Action>(Embed|Merge|None))\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static IList<BlobDirective> LoadDirectives()
        {
            var directives = new List<BlobDirective>();
            directives.Add(new BlobDirective("Release", true, "*", BlobAction.Embed));
            using (var itemReader = File.OpenText("Blobber"))
            {
                for (;;)
                {
                    var line = itemReader.ReadLine();
                    if (line == null)
                        break;

                    var match = DirectiveEx.Match(line);
                    if (!match.Success)
                        continue;

                    var configuration = match.Groups["Configuration"].Success ? match.Groups["Configuration"].Value : null;
                    bool? isPrivate = match.Groups["Scope"].Success ? match.Groups["Scope"].Value == "+" : (bool?)null;
                    var name = match.Groups["Assembly"].Value;
                    var action = (BlobAction)Enum.Parse(typeof(BlobAction), match.Groups["Action"].Value, true);
                    directives.Add(new BlobDirective(configuration, isPrivate ?? true, name, action));
                }
            }
            return directives;
        }
    }
}
