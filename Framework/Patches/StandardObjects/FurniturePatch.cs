using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace PreciseFurniture.Framework.Patches.StandardObjects
{
    internal class FurniturePatch : PatchTemplate
    {
        public static IManifest modManifest;
        internal FurniturePatch(Harmony harmony, IManifest ModManifest) : base(harmony, typeof(Furniture)) 
        { 
            modManifest = ModManifest;
        }
        internal void Apply()
        {
            Patch(true, nameof(Furniture.canBeRemoved), nameof(CanBeRemovedPostFix), [typeof(Farmer)]);
        }

        private static void CanBeRemovedPostFix(Furniture __instance, Farmer who, ref bool __result)
        {
            if (!ModEntry.modConfig.EnableMod)
                return;

            if (ModEntry.modConfig.BlacklistPreventsPickup && __instance.modData.ContainsKey($"{modManifest.UniqueID}/blacklisted"))
            {
                if (__instance.boundingBox.Value.Contains(Game1.viewport.X + Game1.getOldMouseX(), Game1.viewport.Y + Game1.getOldMouseY()))
                {
                    Game1.addHUDMessage(new HUDMessage(I18n.Message_PreciseFurniture_PickupBlacklist(), HUDMessage.error_type) { timeLeft = HUDMessage.defaultTime });
                }
                __result = false;
            }
        }
    }
}
