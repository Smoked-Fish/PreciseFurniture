using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Objects;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using static StardewValley.Objects.BedFurniture;

namespace PreciseFurniture.Framework.Patches.StandardObjects
{
    internal class BedFurniturePatch : PatchTemplate
    {
        public static IManifest modManifest;
        internal BedFurniturePatch(Harmony harmony, IManifest ModManifest) : base(harmony, typeof(BedFurniture))
        {
            modManifest = ModManifest;
        }
        internal void Apply()
        {
            Patch(true, nameof(BedFurniture.canBeRemoved), nameof(CanBeRemovedPostFix), [typeof(Farmer)]);
        }

        // Prevent picking up locked furniture
        private static void CanBeRemovedPostFix(BedFurniture __instance, Farmer who, ref bool __result)
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