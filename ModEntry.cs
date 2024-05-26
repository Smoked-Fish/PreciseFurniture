global using SObject = StardewValley.Object;
using HarmonyLib;
using StardewValley;
using StardewValley.Objects;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using PreciseFurniture.Framework.Patches.StandardObjects;
using PreciseFurniture.Framework.Patches.Farmers;
using Microsoft.Xna.Framework;
using Common.Managers;
using System.Linq;

namespace PreciseFurniture
{
    public class ModEntry : Mod
    {
        // Shared static helpers
        internal static IMonitor monitor;
        internal static IModHelper modHelper;
        internal static ModConfig modConfig;
        private static Harmony harmony;

        public static int ticks = 0;
        public static Furniture furnitureToMove;
        public static bool hasJustMovedFurniture;

        public override void Entry(IModHelper helper)
        {
            // Setup the monitor, helper and multiplayer
            monitor = Monitor;
            modHelper = helper;
            modConfig = Helper.ReadConfig<ModConfig>();
            harmony = new Harmony(this.ModManifest.UniqueID);

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
            ConfigManager.Initialize(ModManifest, modConfig, modHelper, monitor, harmony);
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
                ConfigManager.AddOption(nameof(modConfig.PassableKey));
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
            else if (modConfig.PassableKey.JustPressed())
                SetPassableFurniture();
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

            if (hasJustMovedFurniture)
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
            if (!Context.IsWorldReady)
                return;

            foreach (Furniture f in e.Removed)
            {
                f.modData.Remove($"{this.ModManifest.UniqueID}/blacklisted");
                f.modData.Remove($"{this.ModManifest.UniqueID}/passable");
            }
        }
        private void MoveFurniture(int x, int y)
        {
            int mod = (modConfig.ModKey.IsDown() ? modConfig.ModSpeed : 1);
            Point shift = new Point(x * mod, y * mod);

            Furniture selectedFurniture = GetSelectedFurniture();

            if (furnitureToMove != null)
            {
                selectedFurniture = furnitureToMove;
            }

            if (selectedFurniture != null)
            {
                MoveSelectedFurniture(selectedFurniture, shift);
            }
        }

        private Furniture GetSelectedFurniture()
        {
            var orderedFurniture = Game1.currentLocation.furniture.OrderBy(f => f.furniture_type.Value == 12).ToList();

            foreach (var furniture in orderedFurniture)
            {
                if (furnitureToMove != null) 
                    return null;

                string furnitureName = string.IsNullOrEmpty(furniture.displayName) ? furniture.name : furniture.displayName;
                if (furniture.boundingBox.Value.Contains(Game1.viewport.X + Game1.getOldMouseX(), Game1.viewport.Y + Game1.getOldMouseY()))
                {
                    if (furniture.modData.ContainsKey($"{this.ModManifest.UniqueID}/blacklisted"))
                    {
                        Game1.addHUDMessage(new HUDMessage(I18n.Message("IsBlacklisted", new { furnitureName }), HUDMessage.error_type) { timeLeft = HUDMessage.defaultTime });
                        continue;
                    }
                    return furniture;
                }
            }
            return null;
        }

        private static void MoveSelectedFurniture(Furniture selectedFurniture, Point shift)
        {
            Game1.currentLocation.furniture.Remove(selectedFurniture);
            selectedFurniture.removeLights();
            selectedFurniture.RemoveLightGlow();
            selectedFurniture.boundingBox.Value = new Rectangle(selectedFurniture.boundingBox.Value.Location + shift, selectedFurniture.boundingBox.Value.Size);
            Game1.currentLocation.furniture.Add(selectedFurniture);
            selectedFurniture.updateDrawPosition();

            furnitureToMove = selectedFurniture;
            hasJustMovedFurniture = true;

            if (modConfig.MoveCursor && selectedFurniture.boundingBox.Value.Contains(Game1.viewport.X + Game1.getOldMouseX(), Game1.viewport.Y + Game1.getOldMouseY()))
            {
                Point rawMousePos = Game1.getMousePositionRaw();
                Point newRawMousePos = new Point(rawMousePos.X += shift.X, rawMousePos.Y += shift.Y);
                Game1.setMousePositionRaw(newRawMousePos.X, newRawMousePos.Y);
            }
        }

        private void BlacklistFurniture()
        {
            if (!modConfig.ModKey.IsDown())
                return;

            var orderedFurniture = Game1.currentLocation.furniture.OrderBy(f => f.furniture_type.Value == 12).ToList();

            foreach (var f in orderedFurniture)
            {
                string furnitureName = string.IsNullOrEmpty(f.displayName) ? f.name : f.displayName;
                if (f.boundingBox.Value.Contains(Game1.viewport.X + Game1.getOldMouseX(), Game1.viewport.Y + Game1.getOldMouseY()) && f.hovering)
                {
                    if (f.modData.ContainsKey($"{this.ModManifest.UniqueID}/blacklisted"))
                    {
                        
                        Game1.addHUDMessage(new HUDMessage(I18n.Message("RemoveBlacklist", new { furnitureName }), HUDMessage.stamina_type) { timeLeft = HUDMessage.defaultTime });
                        f.modData.Remove($"{this.ModManifest.UniqueID}/blacklisted");
                        return;
                    }
                    
                    Game1.addHUDMessage(new HUDMessage(I18n.Message("AddBlacklist", new { furnitureName }), HUDMessage.health_type) { timeLeft = HUDMessage.defaultTime });
                    f.modData.Add($"{this.ModManifest.UniqueID}/blacklisted", "T");
                    return;
                }
            }
        }

        private void SetPassableFurniture()
        {
            if (!modConfig.ModKey.IsDown())
                return;

            foreach (var f in Game1.currentLocation.furniture)
            {
                // Ignore rugs as they are already passable
                if (f.furniture_type.Value == 12) continue;

                string furnitureName = string.IsNullOrEmpty(f.displayName) ? f.name : f.displayName;
                if (f.boundingBox.Value.Contains(Game1.viewport.X + Game1.getOldMouseX(), Game1.viewport.Y + Game1.getOldMouseY()) && f.hovering)
                {
                    if (f.modData.ContainsKey($"{this.ModManifest.UniqueID}/passable"))
                    {
                        Game1.addHUDMessage(new HUDMessage(I18n.Message("RemovePassable", new { furnitureName }), HUDMessage.stamina_type) { timeLeft = HUDMessage.defaultTime });
                        f.modData.Remove($"{this.ModManifest.UniqueID}/passable");
                        return;
                    }
                    
                    Game1.addHUDMessage(new HUDMessage(I18n.Message("AddPassable", new { furnitureName }), HUDMessage.health_type) { timeLeft = HUDMessage.defaultTime });
                    f.modData.Add($"{this.ModManifest.UniqueID}/passable", "T");
                    return;
                }
            }
        }
    }
}