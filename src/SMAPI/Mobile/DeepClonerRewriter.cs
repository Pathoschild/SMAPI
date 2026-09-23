using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Force.DeepCloner;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace StardewModdingAPI.Mobile;

internal static class DeepClonerRewriter
{
    static readonly MethodInfo DeepClone_MethodInfo = AccessTools.Method(typeof(DeepClonerExtensions), "DeepClone");

    internal static bool OnRewriterIL(MapMethodWithCallback mapMethod, MethodReference methodReference, ModuleDefinition module, ILProcessor cil, Instruction instruction)
    {
        if (methodReference is GenericInstanceMethod thisMethodGeneric && methodReference.Name.Contains("DeepClone"))
        {
            var DeepClone_MethodImportRef = module.ImportReference(DeepClone_MethodInfo);
            var newDeepCloneMethod = new GenericInstanceMethod(DeepClone_MethodImportRef);
            newDeepCloneMethod.GenericArguments.Add(module.ImportReference(thisMethodGeneric.GenericArguments[0]));
            instruction.Operand = newDeepCloneMethod;
            return true;
        }

        return false;
    }
}
