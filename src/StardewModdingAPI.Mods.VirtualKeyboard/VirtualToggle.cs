using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Mobile;

namespace StardewModdingAPI.Mods.VirtualKeyboard
{
    class VirtualToggle
    {
        private readonly IModHelper helper;
        private readonly IMonitor Monitor;

        private bool enabled = false;
        private ClickableTextureComponent virtualToggleButton;

        private List<KeyButton> keyboard = new List<KeyButton>();
        private ModConfig modConfig;
        private Texture2D texture;

        public VirtualToggle(IModHelper helper, IMonitor monitor)
        {
            this.Monitor = monitor;
            this.helper = helper;
            this.texture = this.helper.Content.Load<Texture2D>("assets/togglebutton.png", ContentSource.ModFolder);
            this.virtualToggleButton = new ClickableTextureComponent(new Rectangle(Game1.toolbarPaddingX + 36, 12, 64, 64), this.texture, new Rectangle(0, 0, 16, 16), 5.75f, false);

            this.modConfig = helper.ReadConfig<ModConfig>();
            for (int i = 0; i < this.modConfig.buttons.Length; i++)
            {
                this.keyboard.Add(new KeyButton(helper, this.modConfig.buttons[i], this.Monitor));
            }
            helper.WriteConfig(this.modConfig);

            this.helper.Events.Display.RenderingHud += this.OnRenderingHUD;
            this.helper.Events.Input.ButtonPressed += this.VirtualToggleButtonPressed;
            this.helper.Events.Input.ButtonReleased += this.VirtualToggleButtonReleased;
        }

        private void VirtualToggleButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!this.enabled && this.shouldTrigger())
            {
                this.enabled = true;
                foreach (var keys in this.keyboard)
                {
                    keys.hidden = false;
                }
            }
            else if (this.enabled && this.shouldTrigger())
            {
                this.enabled = false;
                foreach (var keys in this.keyboard)
                {
                    keys.hidden = true;
                }
            }
        }

        private bool shouldTrigger()
        {
            int x1 = Mouse.GetState().X / (int)Game1.NativeZoomLevel;
            int y1 = Mouse.GetState().Y / (int)Game1.NativeZoomLevel;
            if (this.virtualToggleButton.containsPoint(x1, y1))
            {
                return true;
            }
            return false;
        }

        private void VirtualToggleButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
        }

        private void OnRenderingHUD(object sender, EventArgs e)
        {
            if (Game1.options.verticalToolbar)
                this.virtualToggleButton.bounds.X = Game1.toolbarPaddingX + Game1.toolbar.itemSlotSize + 150;
            else
                this.virtualToggleButton.bounds.X = Game1.toolbarPaddingX + Game1.toolbar.itemSlotSize + 50;
            this.virtualToggleButton.bounds.Y = 10;
            float scale = 1f;
            if (!this.enabled)
            {
                scale = 0.5f;
            }
            if(!Game1.eventUp)
                this.virtualToggleButton.draw(Game1.spriteBatch, Color.White * scale, 0f);
        }
    }
}
