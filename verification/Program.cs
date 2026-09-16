using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length != 2) throw new ArgumentException("Expected mod DLL and Il2CppAssemblies directory.");
        var methods = new Dictionary<string, MethodDefinition>();
        var types = new Dictionary<string, TypeDefinition>();
        var assemblies = new List<AssemblyDefinition>();
        try
        {
            string[] files = Directory.GetFiles(args[1], "*.dll");
            if (files.Length > 512) throw new InvalidOperationException("Interop assembly limit exceeded.");
            foreach (string file in files)
            {
                AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(file);
                assemblies.Add(assembly);
                foreach (TypeDefinition type in assembly.MainModule.Types)
                {
                    types.TryAdd(type.FullName, type);
                    foreach (MethodDefinition method in type.Methods) methods.TryAdd(method.FullName, method);
                }
            }
            AssemblyDefinition mod = AssemblyDefinition.ReadAssembly(args[0]);
            assemblies.Add(mod);
            var pending = new Queue<(MethodDefinition method, string path)>();
            foreach (TypeDefinition type in mod.MainModule.Types)
            {
                foreach (MethodDefinition method in type.Methods)
                {
                    methods[method.FullName] = method;
                    pending.Enqueue((method, method.FullName));
                }
            }
            var seen = new HashSet<string>();
            int failures = 0;
            while (pending.Count > 0)
            {
                if (seen.Count > 30000 || pending.Count > 100000)
                    throw new InvalidOperationException("Interop traversal limit exceeded.");
                var (method, path) = pending.Dequeue();
                if (!seen.Add(method.FullName) || !method.HasBody) continue;
                foreach (Instruction instruction in method.Body.Instructions)
                {
                    string? issue = null;
                    bool map_renderer = method.DeclaringType.FullName is "ClearMinimap.MinimapView" or "ClearMinimap.MapOverlay";
                    if (map_renderer && instruction.Operand is MethodReference draw &&
                        (draw.DeclaringType.FullName == "UnityEngine.GUI" ||
                         (draw.DeclaringType.FullName == "UnityEngine.Graphics" && draw.Name == "DrawTexture")))
                        issue = "Minimap renderer returned to immediate-mode drawing";
                    if (instruction.Operand is string text && text.Contains("unstripping failed"))
                        issue = "Throwing unstripping stub";
                    if (instruction.OpCode == OpCodes.Initobj && instruction.Operand is TypeReference target &&
                        types.TryGetValue(target.FullName, out TypeDefinition? definition) && !definition.IsValueType)
                        issue = "initobj on reference wrapper " + target.FullName;
                    if (issue != null)
                    {
                        Console.Error.WriteLine(issue + ": " + path);
                        failures++;
                    }
                    if (instruction.Operand is MethodReference reference &&
                        methods.TryGetValue(reference.FullName, out MethodDefinition? next))
                        pending.Enqueue((next, path + " -> " + reference.FullName));
                }
            }
            Console.WriteLine($"Inspected {seen.Count} reachable method bodies: {failures} known interop defects.");
            return failures == 0 ? 0 : 1;
        }
        finally
        {
            foreach (AssemblyDefinition assembly in assemblies) assembly.Dispose();
        }
    }
}
