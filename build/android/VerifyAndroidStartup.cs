#:package Mono.Cecil@0.11.6

using Mono.Cecil;
using Mono.Cecil.Cil;

// Fails the Android zip when a rebase drops the mod-load screen hooks.
// Source that merely looks correct is not enough; this reads the built DLL.

if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.Error.WriteLine("Usage: VerifyAndroidStartup <StardewModdingAPI.dll>");
    return 1;
}

string dllPath = Path.GetFullPath(args[0]);
if (!File.Exists(dllPath))
{
    Console.Error.WriteLine($"Android startup invariant check: assembly not found: {dllPath}");
    return 1;
}

var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(Path.GetDirectoryName(dllPath)!);

AssemblyDefinition assembly;
try
{
    assembly = AssemblyDefinition.ReadAssembly(dllPath, new ReaderParameters
    {
        AssemblyResolver = resolver,
        ReadingMode = ReadingMode.Immediate,
        ReadSymbols = false,
        InMemory = true
    });
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Android startup invariant check: could not read {dllPath}");
    Console.Error.WriteLine(ex);
    return 1;
}

using (assembly)
{
    int failures = 0;

    void Fail(string invariant)
    {
        Console.Error.WriteLine("Invariant failed: " + invariant);
        failures++;
    }

    ModuleDefinition module = assembly.MainModule;
    TypeDefinition? score = FindType(module, "StardewModdingAPI.Framework.SCore");
    TypeDefinition? runner = FindType(module, "StardewModdingAPI.Framework.SGameRunner");
    TypeDefinition? loader = FindType(module, "StardewModdingAPI.Mobile.AndroidModLoaderManager");
    TypeDefinition? consoleTool = FindType(module, "StardewModdingAPI.Mobile.MobileConsoleTool");

    MethodDefinition? init = FindMethod(score, "InitializeBeforeFirstAssetLoaded");
    if (init?.Body == null)
    {
        Fail("SCore.InitializeBeforeFirstAssetLoaded must call AndroidModLoaderManager.StartLoggerToScreen before ModBlacklist.CheckLooseFiles and before Task.Run.");
    }
    else
    {
        int startLogger = IndexOfCall(init, "StartLoggerToScreen", "AndroidModLoaderManager");
        int looseFiles = IndexOfCall(init, "CheckLooseFiles", "ModBlacklist");
        int taskRun = IndexOfCall(init, "Run", "System.Threading.Tasks.Task");
        if (startLogger < 0 || looseFiles < 0 || taskRun < 0 || startLogger > looseFiles || startLogger > taskRun)
        {
            Fail("SCore.InitializeBeforeFirstAssetLoaded must call AndroidModLoaderManager.StartLoggerToScreen before ModBlacklist.CheckLooseFiles and before Task.Run.");
        }
    }

    MethodDefinition? updateLambda = FindUpdateLambda(runner);
    if (updateLambda?.Body == null)
    {
        Fail("SGameRunner.Update's compiler-generated <Update>b__0 must have a catch handler and call base Update (GameRunner.Update). Nested methods: " + DescribeNestedMethods(runner));
    }
    else if (!CatchCoversBaseUpdate(updateLambda))
    {
        Fail("SGameRunner.Update's compiler-generated <Update>b__0 must have a catch handler and call base Update (GameRunner.Update). Calls: " + DescribeCalls(updateLambda));
    }

    MethodDefinition? draw = FindMethod(runner, "Draw");
    if (draw?.Body == null || !DrawPaintsOverlayAfterBaseDraw(draw))
    {
        Fail("SGameRunner.Draw must catch GameRunner.Draw / base.Draw and invoke OnAndroidDraw after that try, not only inside it.");
    }

    MethodDefinition? startLoggerMethod = FindMethod(loader, "StartLoggerToScreen");
    if (startLoggerMethod?.Body == null)
    {
        Fail("AndroidModLoaderManager.StartLoggerToScreen must call SGameRunner.RegisterOnDraw before ContentManager.Load of Fonts\\SmallFont.");
    }
    else
    {
        int register = IndexOfCall(startLoggerMethod, "RegisterOnDraw", "SGameRunner");
        int fontLoad = IndexOfFontLoad(startLoggerMethod);
        if (register < 0 || fontLoad < 0 || register > fontLoad)
        {
            Fail("AndroidModLoaderManager.StartLoggerToScreen must call SGameRunner.RegisterOnDraw before ContentManager.Load of Fonts\\SmallFont.");
        }
    }

    MethodDefinition? readLine = FindMethod(consoleTool, "ReadLine");
    if (readLine?.Body == null || HasTightSpin(readLine) || CallsThreadSleep(readLine))
    {
        Fail("MobileConsoleTool.ReadLine must not spin-wait (tight branch back to itself with no blocking call).");
    }

    if (failures != 0)
        return 1;
}

Console.WriteLine("Android startup invariants ok.");
return 0;

static bool CatchCoversBaseUpdate(MethodDefinition lambda)
{
    if (lambda.Body == null)
        return false;

    bool sawProtectedCall = false;
    foreach (Instruction instr in lambda.Body.Instructions)
    {
        if (instr.Operand is not MethodReference called)
            continue;
        if (instr.OpCode.Code is not (Code.Call or Code.Callvirt))
            continue;
        if (!LeadsToGameRunnerUpdate(called, 0, new HashSet<string>()))
            continue;
        if (IsInsideCatchTry(lambda, instr))
            sawProtectedCall = true;
    }

    return sawProtectedCall;
}

static bool LeadsToGameRunnerUpdate(MethodReference reference, int depth, HashSet<string> seen)
{
    if (reference.Name == "Update" && reference.DeclaringType.Name == "GameRunner")
        return true;
    if (depth > 5 || !IsCompilerGenerated(reference))
        return false;

    string key = reference.DeclaringType.FullName + "::" + reference.Name;
    if (!seen.Add(key))
        return false;

    MethodDefinition? definition = TryResolve(reference);
    if (definition?.Body == null)
        return false;

    foreach (Instruction instr in definition.Body.Instructions)
    {
        if (instr.Operand is not MethodReference called)
            continue;
        if (instr.OpCode.Code is not (Code.Call or Code.Callvirt))
            continue;
        if (called.Name == "Update" && called.DeclaringType.Name == "GameRunner")
            return true;
        if (LeadsToGameRunnerUpdate(called, depth + 1, seen))
            return true;
    }

    return false;
}

static bool DrawPaintsOverlayAfterBaseDraw(MethodDefinition draw)
{
    Instruction? baseDraw = null;
    foreach (Instruction instr in draw.Body.Instructions)
    {
        if (instr.Operand is MethodReference called
            && instr.OpCode.Code is Code.Call or Code.Callvirt
            && called.Name == "Draw"
            && called.DeclaringType.Name == "GameRunner")
        {
            baseDraw = instr;
            break;
        }
    }

    if (baseDraw == null)
        return false;

    ExceptionHandler? handler = null;
    int bestSpan = int.MaxValue;
    foreach (ExceptionHandler candidate in draw.Body.ExceptionHandlers)
    {
        if (candidate.HandlerType != ExceptionHandlerType.Catch || candidate.TryStart == null || candidate.TryEnd == null)
            continue;
        if (baseDraw.Offset < candidate.TryStart.Offset || baseDraw.Offset >= candidate.TryEnd.Offset)
            continue;

        int span = candidate.TryEnd.Offset - candidate.TryStart.Offset;
        if (span < bestSpan)
        {
            bestSpan = span;
            handler = candidate;
        }
    }

    if (handler?.TryEnd == null)
        return false;

    bool sawEvent = false;
    bool sawInvoke = false;
    foreach (Instruction instr in draw.Body.Instructions)
    {
        if (instr.Offset < handler.TryEnd.Offset)
            continue;
        if (instr.Operand is FieldReference field && field.Name == "OnAndroidDraw")
            sawEvent = true;
        if (instr.Operand is MethodReference method
            && instr.OpCode.Code is Code.Call or Code.Callvirt
            && (method.Name == "Invoke" || method.Name.Contains("OnAndroidDraw", StringComparison.Ordinal)))
            sawInvoke = true;
    }

    return sawEvent && sawInvoke;
}

static bool HasTightSpin(MethodDefinition method)
{
    foreach (Instruction instr in method.Body.Instructions)
    {
        if (instr.Operand is not Instruction target)
            continue;
        if (!IsBackwardBranch(instr.OpCode.Code))
            continue;
        if (target.Offset > instr.Offset)
            continue;

        bool blockingCall = false;
        foreach (Instruction inner in method.Body.Instructions)
        {
            if (inner.Offset < target.Offset || inner.Offset > instr.Offset)
                continue;
            if (inner.OpCode.Code is not (Code.Call or Code.Callvirt or Code.Calli))
                continue;
            if (inner.Operand is MethodReference called && !IsSpinNoise(called))
            {
                blockingCall = true;
                break;
            }
        }

        if (!blockingCall)
            return true;
    }

    return false;
}

static bool CallsThreadSleep(MethodDefinition method)
{
    foreach (Instruction instr in method.Body.Instructions)
    {
        if (instr.Operand is MethodReference called
            && called.Name == "Sleep"
            && called.DeclaringType.Name == "Thread")
            return true;
    }

    return false;
}

static bool IsSpinNoise(MethodReference method)
{
    string name = method.Name;
    if (name is "IsNullOrEmpty" or "IsNullOrWhiteSpace" or "Enter" or "Exit" or "TryEnter" or "op_Equality" or "op_Inequality")
        return true;
    return name.StartsWith("get_", StringComparison.Ordinal);
}

static bool IsBackwardBranch(Code code)
{
    return code is Code.Br or Code.Br_S
        or Code.Brtrue or Code.Brtrue_S
        or Code.Brfalse or Code.Brfalse_S
        or Code.Beq or Code.Beq_S
        or Code.Bge or Code.Bge_S or Code.Bge_Un or Code.Bge_Un_S
        or Code.Bgt or Code.Bgt_S or Code.Bgt_Un or Code.Bgt_Un_S
        or Code.Ble or Code.Ble_S or Code.Ble_Un or Code.Ble_Un_S
        or Code.Blt or Code.Blt_S or Code.Blt_Un or Code.Blt_Un_S
        or Code.Bne_Un or Code.Bne_Un_S;
}

static bool IsInsideCatchTry(MethodDefinition method, Instruction instr)
{
    foreach (ExceptionHandler handler in method.Body.ExceptionHandlers)
    {
        if (handler.HandlerType != ExceptionHandlerType.Catch || handler.TryStart == null || handler.TryEnd == null)
            continue;
        if (instr.Offset >= handler.TryStart.Offset && instr.Offset < handler.TryEnd.Offset)
            return true;
    }

    return false;
}

static bool IsCompilerGenerated(MethodReference method)
{
    if (method.Name.Contains('<') || method.Name.Contains('>'))
        return true;

    string typeName = method.DeclaringType.Name;
    return typeName.Contains('<') || typeName.Contains("DisplayClass", StringComparison.Ordinal);
}

static int IndexOfCall(MethodDefinition method, string methodName, string declaringTypeName)
{
    int index = 0;
    foreach (Instruction instr in method.Body.Instructions)
    {
        if (instr.Operand is MethodReference called
            && instr.OpCode.Code is Code.Call or Code.Callvirt
            && called.Name == methodName
            && called.DeclaringType.FullName.Contains(declaringTypeName, StringComparison.Ordinal))
            return index;

        index++;
    }

    return -1;
}

static int IndexOfFontLoad(MethodDefinition method)
{
    int index = 0;
    int found = -1;
    foreach (Instruction instr in method.Body.Instructions)
    {
        bool isLoad = instr.Operand is MethodReference called
            && instr.OpCode.Code is Code.Call or Code.Callvirt
            && called.Name == "Load"
            && (called.DeclaringType.Name is "ContentManager" or "LocalizedContentManager");
        bool isFontString = instr.OpCode.Code == Code.Ldstr
            && instr.Operand is string text
            && text.Contains("SmallFont", StringComparison.Ordinal);

        if ((isLoad || isFontString) && found < 0)
            found = index;

        index++;
    }

    return found;
}

static string DescribeCalls(MethodDefinition method)
{
    var names = new List<string>();
    foreach (Instruction instr in method.Body.Instructions)
    {
        if (instr.Operand is MethodReference called && instr.OpCode.Code is Code.Call or Code.Callvirt)
            names.Add(called.DeclaringType.Name + "::" + called.Name);
    }

    return names.Count == 0 ? "(none)" : string.Join(", ", names);
}

static MethodDefinition? FindUpdateLambda(TypeDefinition? runner)
{
    if (runner == null)
        return null;

    foreach (MethodDefinition method in runner.Methods)
    {
        if (method.Name == "<Update>b__0")
            return method;
    }

    foreach (TypeDefinition type in NestedTypes(runner))
    {
        foreach (MethodDefinition method in type.Methods)
        {
            if (method.Name == "<Update>b__0")
                return method;
        }
    }

    return null;
}

static string DescribeNestedMethods(TypeDefinition? runner)
{
    if (runner == null)
        return "(SGameRunner missing)";

    var names = new List<string>();
    foreach (MethodDefinition method in runner.Methods)
    {
        if (method.Name.Contains('<'))
            names.Add(method.Name);
    }

    foreach (TypeDefinition type in NestedTypes(runner))
    {
        foreach (MethodDefinition method in type.Methods)
        {
            if (method.Name.Contains('<') || method.Name.Contains("Update", StringComparison.Ordinal))
                names.Add(type.Name + "::" + method.Name);
        }
    }

    return names.Count == 0 ? "(none)" : string.Join(", ", names);
}

static IEnumerable<TypeDefinition> NestedTypes(TypeDefinition type)
{
    foreach (TypeDefinition nested in type.NestedTypes)
    {
        yield return nested;
        foreach (TypeDefinition child in NestedTypes(nested))
            yield return child;
    }
}

static MethodDefinition? TryResolve(MethodReference reference)
{
    try
    {
        MethodDefinition? resolved = reference.Resolve();
        if (resolved != null)
            return resolved;
    }
    catch
    {
        // The Android output may not resolve every reference. Search this assembly by name.
    }

    if (reference.Module == null)
        return null;

    foreach (TypeDefinition type in AllTypes(reference.Module))
    {
        if (type.FullName != reference.DeclaringType.FullName)
            continue;

        foreach (MethodDefinition method in type.Methods)
        {
            if (method.Name == reference.Name && method.Parameters.Count == reference.Parameters.Count && method.HasBody)
                return method;
        }
    }

    return null;
}

static TypeDefinition? FindType(ModuleDefinition module, string fullName)
{
    foreach (TypeDefinition type in AllTypes(module))
    {
        if (type.FullName == fullName)
            return type;
    }

    return null;
}

static MethodDefinition? FindMethod(TypeDefinition? type, string name)
{
    if (type == null)
        return null;

    foreach (MethodDefinition method in type.Methods)
    {
        if (method.Name == name && method.HasBody)
            return method;
    }

    return null;
}

static IEnumerable<TypeDefinition> AllTypes(ModuleDefinition module)
{
    foreach (TypeDefinition type in module.Types)
    {
        yield return type;
        foreach (TypeDefinition nested in AllNested(type))
            yield return nested;
    }
}

static IEnumerable<TypeDefinition> AllNested(TypeDefinition type)
{
    foreach (TypeDefinition nested in type.NestedTypes)
    {
        yield return nested;
        foreach (TypeDefinition child in AllNested(nested))
            yield return child;
    }
}
