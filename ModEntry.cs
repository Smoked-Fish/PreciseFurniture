using StardewValley;
using StardewValley.Objects;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using PreciseFurniture.Framework.Patches.StandardObjects;
using PreciseFurniture.Framework.Patches.Farmers;
using Microsoft.Xna.Framework;
using Common.Managers;
using System.Linq;
using HarmonyLib;
using System.Threading;
using Common.Helpers;
using Common.Utilities;

namespace PreciseFurniture
{
    public class ModEntry : Mod
    {
        // Shared static helpers
        public static IModHelper ModHelper { get; private set; } = null!;
        public static IMonitor ModMonitor { get; private set; } = null!;
        public static IManifest Manifest { get; private set; } = null!;
        public static Config Config { get; private set; } = null!;

        public static int ticks = 0;
        public static Furniture furnitureToMove;
        public static bool hasJustMovedFurniture;

        public override void Entry(IModHelper helper)
        {
            // Setup the monitor, helper and multiplayer
            ModMonitor = Monitor;
            ModHelper = helper;
            Manifest = ModEntry.Manifest;
            Config = Helper.ReadConfig<Config>();

            ConfigManager.Init(ModEntry.Manifest, Config, Helper, Monitor);
            PatchHelper.Init(new Harmony(Manifest.UniqueID));

            // Apply Farmer patches
            new FarmerPatch().Apply();

            // Apply StandardObject patches
            new FurniturePatch().Apply();
            new BedFurniturePatch().Apply();
            new FishTankFurniturePatch().Apply();

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
            if (!Helper.ModRegistry.IsLoaded("spacechase0.GenericConfigMenu")) return;

            ConfigManager.AddOption(nameof(Config.EnableMod));
            ConfigManager.AddOption(nameof(Config.MoveCursor));
            ConfigManager.AddOption(nameof(Config.BlacklistPreventsPickup));
            ConfigManager.AddOption(nameof(Config.RaiseButton));
            ConfigManager.AddOption(nameof(Config.LowerButton));
            ConfigManager.AddOption(nameof(Config.LeftButton));
            ConfigManager.AddOption(nameof(Config.RightButton));
            ConfigManager.AddOption(nameof(Config.BlacklistKey));
            ConfigManager.AddOption(nameof(Config.PassableKey));
            ConfigManager.AddOption(nameof(Config.ModKey));
            ConfigManager.AddOption(nameof(Config.ModSpeed));
            ConfigManager.AddOption(nameof(Config.MoveSpeed));
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Config.EnableMod || !Context.IsPlayerFree)
                return;
            if (++ticks < Config.MoveSpeed)
                return;

            ticks = 0;

            if (Config.RaiseButton.IsDown())
                MoveFurniture(0, -1);
            else if (Config.LowerButton.IsDown())
                MoveFurniture(0, 1);
            else if (Config.LeftButton.IsDown())
                MoveFurniture(-1, 0);
            else if (Config.RightButton.IsDown())
                MoveFurniture(1, 0);
        }

        private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            if (!Config.EnableMod || !Context.IsWorldReady)
                return;

            if (Config.BlacklistKey.JustPressed())
                BlacklistFurniture();
            else if (Config.PassableKey.JustPressed())
                SetPassableFurniture();
            else if (Config.RaiseButton.JustPressed())
                MoveFurniture(0, -1);
            else if (Config.LowerButton.JustPressed())
                MoveFurniture(0, 1);
            else if (Config.LeftButton.JustPressed())
                MoveFurniture(-1, 0);
            else if (Config.RightButton.JustPressed())
                MoveFurniture(1, 0);
        }

        private void OnCusorMoved(object sender, CursorMovedEventArgs e)
        {
            if (!Config.EnableMod || !Context.IsWorldReady || furnitureToMove == null)
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
                f.modData.Remove($"{Manifest.UniqueID}/blacklisted");
                f.modData.Remove($"{Manifest.UniqueID}/passable");
            }
        }
        private void MoveFurniture(int x, int y)
        {
            int mod = (Config.ModKey.IsDown() ? Config.ModSpeed : 1);
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
                    if (furniture.modData.ContainsKey($"{Manifest.UniqueID}/blacklisted"))
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

            if (Config.MoveCursor && selectedFurniture.boundingBox.Value.Contains(Game1.viewport.X + Game1.getOldMouseX(), Game1.viewport.Y + Game1.getOldMouseY()))
            {
                Point rawMousePos = Game1.getMousePositionRaw();
                Point newRawMousePos = new Point(rawMousePos.X += shift.X, rawMousePos.Y += shift.Y);
                Game1.setMousePositionRaw(newRawMousePos.X, newRawMousePos.Y);
            }
        }

        private void BlacklistFurniture()
        {
            if (!Config.ModKey.IsDown())
                return;

            var orderedFurniture = Game1.currentLocation.furniture.OrderBy(f => f.furniture_type.Value == 12).ToList();

            foreach (var f in orderedFurniture)
            {
                string furnitureName = string.IsNullOrEmpty(f.displayName) ? f.name : f.displayName;
                if (f.boundingBox.Value.Contains(Game1.viewport.X + Game1.getOldMouseX(), Game1.viewport.Y + Game1.getOldMouseY()) && f.hovering)
                {
                    if (f.modData.ContainsKey($"{Manifest.UniqueID}/blacklisted"))
                    {
                        
                        Game1.addHUDMessage(new HUDMessage(I18n.Message("RemoveBlacklist", new { furnitureName }), HUDMessage.stamina_type) { timeLeft = HUDMessage.defaultTime });
                        f.modData.Remove($"{Manifest.UniqueID}/blacklisted");
                        return;
                    }
                    
                    Game1.addHUDMessage(new HUDMessage(I18n.Message("AddBlacklist", new { furnitureName }), HUDMessage.health_type) { timeLeft = HUDMessage.defaultTime });
                    f.modData.Add($"{Manifest.UniqueID}/blacklisted", "T");
                    return;
                }
            }
        }

        private void SetPassableFurniture()
        {
            if (!Config.ModKey.IsDown())
                return;

            foreach (var f in Game1.currentLocation.furniture)
            {
                // Ignore rugs as they are already passable
                if (f.furniture_type.Value == 12) continue;

                string furnitureName = string.IsNullOrEmpty(f.displayName) ? f.name : f.displayName;
                if (f.boundingBox.Value.Contains(Game1.viewport.X + Game1.getOldMouseX(), Game1.viewport.Y + Game1.getOldMouseY()) && f.hovering)
                {
                    if (f.modData.ContainsKey($"{Manifest.UniqueID}/passable"))
                    {
                        Game1.addHUDMessage(new HUDMessage(I18n.Message("RemovePassable", new { furnitureName }), HUDMessage.stamina_type) { timeLeft = HUDMessage.defaultTime });
                        f.modData.Remove($"{Manifest.UniqueID}/passable");
                        return;
                    }
                    
                    Game1.addHUDMessage(new HUDMessage(I18n.Message("AddPassable", new { furnitureName }), HUDMessage.health_type) { timeLeft = HUDMessage.defaultTime });
                    f.modData.Add($"{Manifest.UniqueID}/passable", "T");
                    return;
                }
            }
        }
    }
}