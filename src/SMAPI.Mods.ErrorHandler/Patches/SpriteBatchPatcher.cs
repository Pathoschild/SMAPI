using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Internal.Patching;

namespace StardewModdingAPI.Mods.ErrorHandler.Patches
{
    /// <summary>Harmony patches for <see cref="SpriteBatch"/> which validate textures earlier.</summary>
    /// <remarks>Patch methods must be static for Harmony to work correctly. See the Harmony documentation before renaming patch arguments.</remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    internal class SpriteBatchPatcher : BasePatcher
    {
        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
        public override void Apply(Harmony harmony, IMonitor monitor)
        {
            harmony.Patch(
                original: this.RequireMethod<SpriteBatch>("CheckValid", new[] { typeof(Texture2D) }),
                postfix: this.GetHarmonyMethod(nameof(SpriteBatchPatcher.After_CheckValid))
            );
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The method to call after <see cref="SpriteBatch.CheckValid"/>.</summary>
        /// <param name="texture">The texture to validate.</param>
        private static void After_CheckValid(Texture2D texture)
        {
            if (texture?.IsDisposed == true)
                throw new ObjectDisposedException("Cannot draw this texture because it's disposed.");
        }
    }
}
