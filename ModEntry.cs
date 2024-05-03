using HarmonyLib;
using StardewValley;
using StardewValley.Objects;
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

namespace PreciseFurniture
{
    public class ModEntry : Mod
    {
        // Shared static helpers
        internal static IMonitor monitor;
        internal static IModHelper modHelper;
        internal static ModConfig modConfig;
        internal static Multiplayer multiplayer;

        // Managers
        internal static ApiManager apiManager;

        public static int ticks = 0;
public static Furniture movedFurniture;
        public static Vector2 testMouse = Vector2.Zero;
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

            // Setup the manager
            apiManager = new ApiManager();


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
            var configApi = apiManager.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu", false);
            if (Helper.ModRegistry.IsLoaded("spacechase0.GenericModConfigMenu") && configApi != null)
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
{
                Monitor.Log($"Error: Property '{name}' not found in ModConfig.", LogLevel.Error);
                return;
}

            Func<string> getName = () => I18n.GetByKey($"Config.{typeof(ModEntry).Namespace}.{name}.Name");
            Func<string> getDescription = () => I18n.GetByKey($"Config.{typeof(ModEntry).Namespace}.{name}.Description");

            if (getName == null || getDescription == null)
{
                Monitor.Log($"Error: Localization keys for '{name}' not found.", LogLevel.Error);
                return;
}

            var getterMethod = propertyInfo.GetGetMethod();
            var setterMethod = propertyInfo.GetSetMethod();

            if (getterMethod == null || setterMethod == null)
            {
                Monitor.Log($"Error: The get/set methods are null for property '{name}'.", LogLevel.Error);
                return;
            }

            var getter = Delegate.CreateDelegate(typeof(Func<>).MakeGenericType(propertyInfo.PropertyType), modConfig, getterMethod);
            var setter = Delegate.CreateDelegate(typeof(Action<>).MakeGenericType(propertyInfo.PropertyType), modConfig, setterMethod);

            switch (propertyInfo.PropertyType.Name)
            {
                case nameof(Boolean):
                    configApi.AddBoolOption(ModManifest, (Func<bool>)getter, (Action<bool>)setter, getName, getDescription);
            break;
                case nameof(Int32):
                    configApi.AddNumberOption(ModManifest, (Func<int>)getter, (Action<int>)setter, getName, getDescription);
                    break;
                case nameof(Single):
                    configApi.AddNumberOption(ModManifest, (Func<float>)getter, (Action<float>)setter, getName, getDescription);
            break;
                case nameof(String):
                    configApi.AddTextOption(ModManifest, (Func<string>)getter, (Action<string>)setter, getName, getDescription);
                    break;
                case nameof(SButton):
                    configApi.AddKeybind(ModManifest, (Func<SButton>)getter, (Action<SButton>)setter, getName, getDescription);
            break;
                case nameof(KeybindList):
                    configApi.AddKeybindList(ModManifest, (Func<KeybindList>)getter, (Action<KeybindList>)setter, getName, getDescription);
                    break;
                default:
                    Monitor.Log($"Error: Unsupported property type '{propertyInfo.PropertyType.Name}' for '{name}'.", LogLevel.Error);
                    break;
            }
        }
    }
}