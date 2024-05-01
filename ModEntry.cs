using HarmonyLib;
using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using PreciseFurniture.Framework.Managers;
using PreciseFurniture.Framework.Interfaces;
using PreciseFurniture.Framework.Patches.StandardObjects;
using PreciseFurniture.Framework.Patches.Farmers;
using Microsoft.Xna.Framework;
using System.Reflection;
using System;
using StardewValley.Objects;

namespace PreciseFurniture
{
    public class ModEntry : Mod
    {
        // Shared static helpers
        internal static IMonitor monitor;
        internal static IModHelper modHelper;
        internal static Multiplayer multiplayer;
        internal static ModConfig modConfig;

        // Managers
        internal static ApiManager apiManager;

        public static int ticks = 0;

        public override void Entry(IModHelper helper)
        {
            // Setup i18n
            I18n.Init(helper.Translation);

            // Setup the monitor, helper and multiplayer
            monitor = Monitor;
            modHelper = helper;
            multiplayer = helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();

            // Setup the manager
            apiManager = new ApiManager(monitor);

            // Load the Harmony patches
            try
            {
                var harmony = new Harmony(this.ModManifest.UniqueID);

                // Apply Farmer patches
                new FarmerPatch(harmony).Apply();

                // Apply StandardObject patches
                new FurniturePatch(harmony, this.ModManifest).Apply();
                new BedFurniturePatch(harmony, this.ModManifest).Apply();
                new FishTankFurniturePatch(harmony).Apply();

            }
            catch (Exception e)
            {
                Monitor.Log($"Issue with Harmony patching: {e}", LogLevel.Error);
                return;
            }

            // Hook into GameLoop events
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;

            // Hook into Input events
            helper.Events.Input.ButtonsChanged += OnButtonsChanged;

            // Hook into World events
            helper.Events.World.FurnitureListChanged += OnFurnitureListChanged;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            modConfig = Helper.ReadConfig<ModConfig>();

            if (Helper.ModRegistry.IsLoaded("spacechase0.GenericModConfigMenu") && apiManager.HookIntoGenericModConfigMenu(Helper))
            {
                var configApi = apiManager.GetGenericModConfigMenuApi();
                configApi.Register(ModManifest, () => modConfig = new ModConfig(), () => Helper.WriteConfig(modConfig));

                AddOption(configApi, nameof(modConfig.EnableMod));
                AddOption(configApi, nameof(modConfig.MoveCursor));
                AddOption(configApi, nameof(modConfig.BlacklistPreventsPickup));
                AddOption(configApi, nameof(modConfig.RaiseButton));
                AddOption(configApi, nameof(modConfig.LowerButton));
                AddOption(configApi, nameof(modConfig.LeftButton));
                AddOption(configApi, nameof(modConfig.RightButton));
                AddOption(configApi, nameof(modConfig.BlacklistKey));
                AddOption(configApi, nameof(modConfig.ModKey));
                AddOption(configApi, nameof(modConfig.ModSpeed));
                AddOption(configApi, nameof(modConfig.MoveSpeed));
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

        private void OnFurnitureListChanged(object sender, FurnitureListChangedEventArgs e)
        {
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
            foreach (var f in Game1.currentLocation.furniture)
            {
                string displayName = string.IsNullOrEmpty(f.displayName) ? f.name : f.displayName;
                if (f.boundingBox.Value.Contains(Game1.viewport.X + Game1.getOldMouseX(), Game1.viewport.Y + Game1.getOldMouseY()))
                {
                    if (f.modData.ContainsKey($"{this.ModManifest.UniqueID}/blacklisted"))
                    {
                        Game1.addHUDMessage(new HUDMessage(I18n.Message_PreciseFurniture_IsBlacklisted(displayName), HUDMessage.error_type) { timeLeft = HUDMessage.defaultTime });
                        continue;
                    }


                    Game1.currentLocation.furniture.Remove(f);
                    f.removeLights();
                    f.RemoveLightGlow();
                    f.boundingBox.Value = new Rectangle(f.boundingBox.Value.Location + shift, f.boundingBox.Value.Size);
                    Game1.currentLocation.furniture.Add(f);
                    f.updateDrawPosition();

                    if (modConfig.MoveCursor)
                        Game1.setMousePosition(Game1.getOldMouseX() + shift.X, Game1.getOldMouseY() + shift.Y);

                    return;
                }
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

        private void AddOption(IGenericModConfigMenuApi configApi, string name)
        {
            PropertyInfo propertyInfo = typeof(ModConfig).GetProperty(name);
            if (propertyInfo == null)
                return;

            Func<string> getName = () => I18n.GetByKey($"Config.{typeof(ModEntry).Namespace}.{name}.Name");
            Func<string> getDescription = () => I18n.GetByKey($"Config.{typeof(ModEntry).Namespace}.{name}.Description");

            if (getName == null || getDescription == null)
                return;

            if (propertyInfo.PropertyType == typeof(bool))
            {
                Func<bool> getter = () => (bool)propertyInfo.GetValue(modConfig);
                Action<bool> setter = value => propertyInfo.SetValue(modConfig, value);
                configApi.AddBoolOption(ModManifest, getter, setter, getName, getDescription);
            }
            else if (propertyInfo.PropertyType == typeof(int))
            {
                Func<int> getter = () => (int)propertyInfo.GetValue(modConfig);
                Action<int> setter = value => propertyInfo.SetValue(modConfig, value);
                configApi.AddNumberOption(ModManifest, getter, setter, getName, getDescription);
            }
            else if (propertyInfo.PropertyType == typeof(float))
            {
                Func<float> getter = () => (float)propertyInfo.GetValue(modConfig);
                Action<float> setter = value => propertyInfo.SetValue(modConfig, value);
                configApi.AddNumberOption(ModManifest, getter, setter, getName, getDescription);
            }
            else if (propertyInfo.PropertyType == typeof(string))
            {
                Func<string> getter = () => (string)propertyInfo.GetValue(modConfig);
                Action<string> setter = value => propertyInfo.SetValue(modConfig, value);
                configApi.AddTextOption(ModManifest, getter, setter, getName, getDescription);
            }
            else if (propertyInfo.PropertyType == typeof(SButton))
            {
                Func<SButton> getter = () => (SButton)propertyInfo.GetValue(modConfig);
                Action<SButton> setter = value => propertyInfo.SetValue(modConfig, value);
                configApi.AddKeybind(ModManifest, getter, setter, getName, getDescription);
            }
            else if (propertyInfo.PropertyType == typeof(KeybindList))
            {
                Func<KeybindList> getter = () => (KeybindList)propertyInfo.GetValue(modConfig);
                Action<KeybindList> setter = value => propertyInfo.SetValue(modConfig, value);
                configApi.AddKeybindList(ModManifest, getter, setter, getName, getDescription);
            }
        }
    }
}