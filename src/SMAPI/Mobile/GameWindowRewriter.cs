using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace StardewModdingAPI.Mobile
{
    internal static class GameWindowRewriter
    {
        public static readonly Type ThisType = typeof(GameWindowRewriter);

        const string add_KeyDown_MethodFullName = "System.Void Microsoft.Xna.Framework.GameWindow::add_KeyDown(System.EventHandler`1<Microsoft.Xna.Framework.InputKeyEventArgs>)";

        const string remove_KeyDown_MethodFullName = "System.Void Microsoft.Xna.Framework.GameWindow::remove_KeyDown(System.EventHandler`1<Microsoft.Xna.Framework.InputKeyEventArgs>)";

        internal static bool OnRewriteIL(MapMethodWithCallback mapMethod, MethodReference methodReference, ModuleDefinition module, ILProcessor cil, Instruction instruction)
        {
            if (methodReference.FullName == add_KeyDown_MethodFullName || methodReference.FullName == remove_KeyDown_MethodFullName)
            {
                //instruction.Operand = module.ImportReference(AccessTools.Method(ThisType, nameof(add_remove_KeyDown_Disable)));
                instruction.OpCode = OpCodes.Nop;
                instruction.Operand = null;
                return true;
            }

            return false;
        }

        static void add_remove_KeyDown_Disable(this GameWindow obj, EventHandler<InputKeyEventArgs> inputkeyEvent)
        {
            Console.WriteLine("On add_remove_KeyDown_Disable(...)");
        }
    }
}
