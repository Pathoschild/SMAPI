using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewValley;

namespace StardewModdingAPI.Mobile;

internal static class KeyboardInputRewriter
{
    public delegate void KeyEventHandler(object sender, KeyEventArgs e);

    internal static bool OnRewriteIL(MapMethodWithCallback mapMethod, MethodReference methodReference, ModuleDefinition module, ILProcessor cil, Instruction instruction)
    {
        if (methodReference.FullName.Contains("remove_KeyDown")
            || methodReference.FullName.Contains("add_KeyDown"))
        {
            instruction.OpCode = OpCodes.Nop;
            instruction.Operand = null;
            return true;
        }

        return false;
    }
}

internal static class KeyEventHandlerRewriter
{
    public static void Ctor(object sender, int ptr)
    {
        Console.WriteLine($"On called KeyEventHandler ctor({sender}, ${ptr})");
    }
}
