using System.Reflection;
using System.Runtime.Loader;
using Mono.Cecil;

const string guid = "NorskIT.ProximityAndGlobalVoiceChat";
const string name = "ProximityAndGlobalVoiceChat";
const string version = "0.2.2";
const string translation = "ProximityVoiceChat.translations.English.yml";
if (args.Length < 2) throw new ArgumentException("Usage: VerifyFork identity|conflicts|references <dll-or-plugin-directory> [original-dll | managed-dir core-dir]");

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static IEnumerable<string> PluginIds(string file)
{
    // Cecil reads metadata without executing plugins or holding DLLs open.
    using var module = ModuleDefinition.ReadModule(file);
    return module.Types.SelectMany(t => t.CustomAttributes)
        .Where(a => a.AttributeType.FullName == "BepInEx.BepInPlugin")
        .Select(a => (string)a.ConstructorArguments[0].Value).ToArray();
}

if (args[0] == "unchanged-output")
{
    Require(args.Length == 3, "Expected unchanged-output <old-dll> <new-dll>.");
    using var oldModule = ModuleDefinition.ReadModule(args[1]);
    using var newModule = ModuleDefinition.ReadModule(args[2]);
    int count = 0;
    foreach (string typeName in new[] { "PlaybackBuffer", "PlaybackResampler", "NormalizedAudioResampler", "VoiceAudioOutput", "NativeAudio", "NativeOpus", "NoiseSuppressor", "VoiceEncoder" })
    {
        string fullName = "ProximityVoiceChat.Voice.Audio." + typeName;
        var before = oldModule.GetType(fullName); var after = newModule.GetType(fullName);
        Require(before != null && after != null, "Missing output type " + fullName);
        Require(before!.Methods.Count == after!.Methods.Count, "Changed method count " + fullName);
        foreach (var method in before.Methods.Where(m => m.HasBody))
        {
            var current = after.Methods.Single(m => m.FullName == method.FullName);
            Require(method.Body.Instructions.Select(i => i.ToString()).SequenceEqual(current.Body.Instructions.Select(i => i.ToString())), "Changed global/monitor core: " + method.FullName);
            count++;
        }
    }
    Console.WriteLine($"PASS: {count} global/monitor output and codec method bodies unchanged from baseline.");
    return;
}

if (args[0] == "references")
{
    Require(args.Length == 4, "Expected references <dll> <game-managed-directory> <bepinex-core-directory>.");
    using var resolver = new DefaultAssemblyResolver();
    resolver.AddSearchDirectory(args[2]); resolver.AddSearchDirectory(args[3]);
    using var module = ModuleDefinition.ReadModule(args[1], new ReaderParameters { AssemblyResolver = resolver });
    int count = 0;
    foreach (var member in module.GetMemberReferences())
    {
        string scope = member.DeclaringType.Scope.Name;
        if (!(scope.StartsWith("assembly_") || scope.StartsWith("Unity") || scope is "BepInEx" or "0Harmony" or "Splatform" or "com.rlabrecque.steamworks.net")) continue;
        Require(member.Resolve() != null, $"Unresolved runtime member: {member.FullName}");
        count++;
    }
    IEnumerable<TypeDefinition> Types(IEnumerable<TypeDefinition> types) => types.SelectMany(t => new[] { t }.Concat(Types(t.NestedTypes)));
    int patches = 0;
    foreach (var attribute in Types(module.Types).SelectMany(t => t.CustomAttributes.Concat(t.Methods.SelectMany(m => m.CustomAttributes))))
    {
        if (attribute.AttributeType.FullName != "HarmonyLib.HarmonyPatch" || attribute.ConstructorArguments.Count < 2) continue;
        if (attribute.ConstructorArguments[0].Value is not TypeReference target || attribute.ConstructorArguments[1].Value is not string method) continue;
        Require(target.Resolve().Methods.Any(m => m.Name == method), $"Missing Harmony target: {target.FullName}.{method}");
        patches++;
    }
    Console.WriteLine($"PASS: {count} game/BepInEx member references and {patches} declared Harmony targets resolve against installed assemblies.");
    return;
}

if (args[0] == "conflicts")
{
    var matches = new List<string>();
    foreach (string file in Directory.EnumerateFiles(args[1], "*.dll", SearchOption.AllDirectories))
    {
        try
        {
            if (PluginIds(file).Any(id => id is guid or "Azumatt.ProximityVoiceChat"))
                matches.Add(Path.GetFullPath(file));
        }
        catch (BadImageFormatException) { /* Native DLL. */ }
    }
    foreach (string file in matches) Console.WriteLine(file);
    return;
}

using (var module = ModuleDefinition.ReadModule(args[1]))
{
    Require(module.Assembly.Name.Name == name, "Wrong assembly identity.");
    Require(module.Assembly.Name.Version == new Version(0, 2, 2, 0), "Wrong assembly version.");
    var plugins = module.Types.SelectMany(t => t.CustomAttributes)
        .Where(a => a.AttributeType.FullName == "BepInEx.BepInPlugin").ToArray();
    Require(plugins.Length == 1, "Expected exactly one plugin.");
    Require(plugins[0].ConstructorArguments.Select(a => (string)a.Value)
        .SequenceEqual(new[] { guid, name, version }), "Wrong BepInEx identity.");
    Require(module.Resources.OfType<EmbeddedResource>().Any(r => r.Name == translation && r.GetResourceData().Length > 0), "Missing translation resource.");
    Require(!module.AssemblyReferences.Any(r => r.Name.StartsWith("AsmResolver") || r.Name.StartsWith("BepInEx.AssemblyPublicizer") || r.Name == "ProximityVoiceChat"), "Unexpected runtime dependency.");
}
Console.WriteLine("PASS: assembly identity, single plugin, translations, runtime dependency boundaries.");
Require(args[0] == "identity", "Unknown verification mode. Audio regression tests are in tools/TestAudio.");
