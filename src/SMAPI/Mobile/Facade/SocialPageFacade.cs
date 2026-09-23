using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework.ModLoading.Rewriters;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Mobile.Facade;

public class SocialPageFacade : SocialPage, IRewriteFacade
{
    public SocialPageFacade(int x, int y, int width, int height) : base(x, y, width, height)
    {
    }

    /// <summary>Draw the heart sprite for an NPC's entry in the social page.</summary>
    /// <param name="b">The sprite batch being drawn.</param>
    /// <param name="npcIndex">The index of the NPC in <see cref="F:StardewValley.Menus.SocialPage.sprites" />.</param>
    /// <param name="entry">The NPC's cached social data.</param>
    /// <param name="hearts">The current heart index being drawn (starting at 0 for the first heart).</param>
    /// <param name="isDating">Whether the player is currently dating this NPC.</param>
    /// <param name="isCurrentSpouse">Whether the player is currently married to this NPC.</param>
    public void drawNPCSlotHeart(SpriteBatch b, int npcIndex, SocialEntry entry, int hearts, bool isDating, bool isCurrentSpouse)
    {
        bool isLockedHeart = entry.IsDatable && !isDating && !isCurrentSpouse && hearts >= 8;
        int heartX = ((hearts < entry.HeartLevel || isLockedHeart) ? 211 : 218);
        Color heartTint = ((hearts < 10 && isLockedHeart) ? (Color.Black * 0.35f) : Color.White);
        if (hearts < 10)
        {
            b.Draw(Game1.mouseCursors,
                new Vector2(this.xPositionOnScreen + 320 - 4 + hearts * 32,
                this.sprites[npcIndex].bounds.Y + 64 - 28),
                new Rectangle(heartX, 428, 7, 6), heartTint, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);
        }
        else
        {
            b.Draw(Game1.mouseCursors,
                new Vector2(this.xPositionOnScreen + 320 - 4 + (hearts - 10) * 32,
                this.sprites[npcIndex].bounds.Y + 64),
                new Rectangle(heartX, 428, 7, 6), heartTint, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);
        }
    }
}
