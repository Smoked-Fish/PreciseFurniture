using HarmonyLib;
using StardewValley;
using StardewValley.Objects;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using PreciseFurniture.Framework.Patches.StandardObjects;
using PreciseFurniture.Framework.Patches.Farmers;
using Microsoft.Xna.Framework;
using System.Reflection;
using System;
using Common.Managers;

namespace PreciseFurniture
{
    public class ModEntry : Mod
    {
        // Shared static helpers
        internal static IMonitor monitor;
        internal static IModHelper modHelper;
        internal static ModConfig modConfig;
        internal static Multiplayer multiplayer;

        public static int ticks = 0;
        public static Furniture furnitureToMove;
        public static bool hasJustMovedFurniture;

        public override void Entry(IModHelper helper)
        {
            // Setup i18n
            I18n.Init(helper.Translation);

            // Setup the monitor, helper and multiplayer
            monitor = Monitor;
            modHelper = helper;
            modConfig = Helper.ReadConfig<ModConfig>();
            multiplayer = helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();

            var harmony = new Harmony(this.ModManifest.UniqueID);

            // Apply Farmer patches
            new FarmerPatch(harmony).Apply();

            // Apply StandardObject patches
            new FurniturePatch(harmony, this.ModManifest).Apply();
            new BedFurniturePatch(harmony, this.ModManifest).Apply();
            new FishTankFurniturePatch(harmony).Apply();

            // Hook into GameLoop events
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;

            // Hook into Input events
            helper.Events.Input.ButtonsChanged += OnButtonsChanged;
            helper.Events.Input.CursorMoved += OnCusorMoved;

            // Hook into World events
            helper.Events.World.FurnitureListChanged += OnFurnitureListChanged;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            ConfigManager.Initialize(ModManifest, modConfig, modHelper, monitor);
            if (Helper.ModRegistry.IsLoaded("spacechase0.GenericModConfigMenu"))
            {
                ConfigManager.AddOption(nameof(modConfig.EnableMod));
                ConfigManager.AddOption(nameof(modConfig.MoveCursor));
                ConfigManager.AddOption(nameof(modConfig.BlacklistPreventsPickup));
                ConfigManager.AddOption(nameof(modConfig.RaiseButton));
                ConfigManager.AddOption(nameof(modConfig.LowerButton));
                ConfigManager.AddOption(nameof(modConfig.LeftButton));
                ConfigManager.AddOption(nameof(modConfig.RightButton));
                ConfigManager.AddOption(nameof(modConfig.BlacklistKey));
                ConfigManager.AddOption(nameof(modConfig.ModKey));
                ConfigManager.AddOption(nameof(modConfig.ModSpeed));
                ConfigManager.AddOption(nameof(modConfig.MoveSpeed));
            }
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!modConfig.EnableMod || !Context.IsPlayerFree)
                return;
            if (++ticks < modConfig.MoveSpeed)
                return;

            ticks = 0;

            if (modConfig.RaiseButton.IsDown())
                MoveFurniture(0, -1);
            else if (modConfig.LowerButton.IsDown())
                MoveFurniture(0, 1);
            else if (modConfig.LeftButton.IsDown())
                MoveFurniture(-1, 0);
            else if (modConfig.RightButton.IsDown())
                MoveFurniture(1, 0);
        }

        private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            if (!modConfig.EnableMod || !Context.IsWorldReady)
                return;

            if (modConfig.BlacklistKey.JustPressed())
                BlacklistFurniture();
            else if (modConfig.RaiseButton.JustPressed())
                MoveFurniture(0, -1);
            else if (modConfig.LowerButton.JustPressed())
                MoveFurniture(0, 1);
            else if (modConfig.LeftButton.JustPressed())
                MoveFurniture(-1, 0);
            else if (modConfig.RightButton.JustPressed())
                MoveFurniture(1, 0);
        }

        private void OnCusorMoved(object sender, CursorMovedEventArgs e)
        {
            if (!modConfig.EnableMod || !Context.IsWorldReady || furnitureToMove == null)
                return;

            if (furnitureToMove.boundingBox.Value.Contains(Game1.viewport.X + e.NewPosition.ScreenPixels.X, Game1.viewport.Y + e.NewPosition.ScreenPixels.Y) && hasJustMovedFurniture)
            {
                hasJustMovedFurniture = false;
            }
            else
            {
                furnitureToMove = null;
            }
        }

        private void OnFurnitureListChanged(object sender, FurnitureListChangedEventArgs e)
        {
            if (!modConfig.EnableMod || !Context.IsWorldReady)
                return;

            foreach (Furniture f in e.Removed)
            {
                if (f.modData.ContainsKey($"{this.ModManifest.UniqueID}/blacklisted"))
                {
                    f.modData.Remove($"{this.ModManifest.UniqueID}/blacklisted");
                }
            }
        }

        private void MoveFurniture(int x, int y)
        {
            int mod = (modConfig.ModKey.IsDown() ? modConfig.ModSpeed : 1);
            Point shift = new Point(x * mod, y * mod);

            Furniture selectedFurniture = GetSelectedFurniture(shift);

            if (selectedFurniture == null && furnitureToMove != null)
            {
                selectedFurniture = furnitureToMove;
            }

            if (selectedFurniture != null)
            {
                MoveSelectedFurniture(selectedFurniture, shift);
            }
        }

        private Furniture GetSelectedFurniture(Point shift)
        {
            foreach (var furniture in Game1.currentLocation.furniture)
            {
                if (furnitureToMove != null && furnitureToMove.boundingBox.Value.Contains(Game1.viewport.X + Game1.getOldMouseX(), Game1.viewport.Y + Game1.getOldMouseY()))
                {
                    return null;
                }

                string displayName = string.IsNullOrEmpty(furniture.displayName) ? furniture.name : furniture.displayName;
                if (furniture.boundingBox.Value.Contains(Game1.viewport.X + Game1.getOldMouseX(), Game1.viewport.Y + Game1.getOldMouseY()))
                {
                    if (furniture.modData.ContainsKey($"{this.ModManifest.UniqueID}/blacklisted"))
                    {
                        Game1.addHUDMessage(new HUDMessage(I18n.Message_PreciseFurniture_IsBlacklisted(displayName), HUDMessage.error_type) { timeLeft = HUDMessage.defaultTime });
                        continue;
                    }
                    return furniture;
                }
            }
            return null;
        }

        private void MoveSelectedFurniture(Furniture selectedFurniture, Point shift)
        {
            Game1.currentLocation.furniture.Remove(selectedFurniture);
            selectedFurniture.removeLights();
            selectedFurniture.RemoveLightGlow();
            selectedFurniture.boundingBox.Value = new Rectangle(selectedFurniture.boundingBox.Value.Location + shift, selectedFurniture.boundingBox.Value.Size);
            Game1.currentLocation.furniture.Add(selectedFurniture);
            selectedFurniture.updateDrawPosition();

            if (modConfig.MoveCursor && selectedFurniture.boundingBox.Value.Contains(Game1.viewport.X + Game1.getOldMouseX(), Game1.viewport.Y + Game1.getOldMouseY()))
            {
                furnitureToMove = selectedFurniture;
                Point rawMousePos = Game1.getMousePositionRaw();
                Point newRawMousePos = new Point(rawMousePos.X += shift.X, rawMousePos.Y += shift.Y);
                Game1.setMousePositionRaw(newRawMousePos.X, newRawMousePos.Y);
                hasJustMovedFurniture = true;
            }
        }

        private void BlacklistFurniture()
        {
            if (!modConfig.ModKey.IsDown())
                return;

            foreach (var f in Game1.currentLocation.furniture)
            {
                string displayName = string.IsNullOrEmpty(f.displayName) ? f.name : f.displayName;
                if (f.boundingBox.Value.Contains(Game1.viewport.X + Game1.getOldMouseX(), Game1.viewport.Y + Game1.getOldMouseY()))
                {
                    if (f.modData.ContainsKey($"{this.ModManifest.UniqueID}/blacklisted"))
                    {
                        Game1.addHUDMessage(new HUDMessage(I18n.Message_PreciseFurniture_RemoveBlacklist(displayName), HUDMessage.stamina_type) { timeLeft = HUDMessage.defaultTime });
                        f.modData.Remove($"{this.ModManifest.UniqueID}/blacklisted");
                        return;
                    }


                    Game1.addHUDMessage(new HUDMessage(I18n.Message_PreciseFurniture_AddBlacklist(displayName), HUDMessage.health_type) { timeLeft = HUDMessage.defaultTime });
                    f.modData.Add($"{this.ModManifest.UniqueID}/blacklisted", "T");
                    return;
                }
            }
        }
    }
}