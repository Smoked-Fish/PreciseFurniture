using Common.Helpers;
using Common.Utilities;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;

namespace PreciseFurniture.Framework.Patches.StandardObjects;
internal sealed class BedFurniturePatch() : PatchHelper(typeof(BedFurniture))
{
    internal void Apply()
    {
        Patch(PatchType.Postfix, nameof(BedFurniture.canBeRemoved), nameof(CanBeRemovedPostFix), [typeof(Farmer)]);
        Patch(PatchType.Postfix, nameof(BedFurniture.IntersectsForCollision), nameof(IntersectsForCollisionPostfix), [typeof(Rectangle)]);
    }

    // Prevent picking up locked furniture
    private static void CanBeRemovedPostFix(BedFurniture __instance, Farmer who, ref bool __result)
    {
        if (!ModEntry.Config.EnableMod)
            return;

        if (!ModEntry.Config.BlacklistPreventsPickup || !__instance.modData.ContainsKey($"{ModEntry.Manifest.UniqueID}/blacklisted"))
            return;

        if (__instance.boundingBox.Value.Contains(Game1.viewport.X + Game1.getOldMouseX(), Game1.viewport.Y + Game1.getOldMouseY()))
        {
            Game1.addHUDMessage(new HUDMessage(I18n.Message("PickupBlacklist"), HUDMessage.error_type) { timeLeft = HUDMessage.defaultTime });
        }
        __result = false;
    }

    // Make beds passable
    private static void IntersectsForCollisionPostfix(Furniture __instance, Rectangle rect, ref bool __result)
    {
        if (!ModEntry.Config.EnableMod)
            return;

        if (__instance.modData.ContainsKey($"{ModEntry.Manifest.UniqueID}/passable"))
        {
            __result = false;
        }
    }
}