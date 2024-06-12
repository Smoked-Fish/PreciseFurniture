using Common.Helpers;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;

namespace PreciseFurniture.Framework.Patches.StandardObjects;
internal sealed class FishTankFurniturePatch() : PatchHelper(typeof(FishTankFurniture))
{
    public void Apply()
    {
        Patch(PatchType.Postfix, nameof(FishTankFurniture.GetTankBounds), nameof(GetTankBoundsPostfix));
    }

    private static void GetTankBoundsPostfix(FishTankFurniture __instance, ref Rectangle __result)
    {
        if (!ModEntry.Config.EnableMod)
            return;

        Rectangle rectangle = ItemRegistry.GetDataOrErrorItem(__instance.QualifiedItemId).GetSourceRect();
        int height = rectangle.Height / 16;
        int width = rectangle.Width / 16;
        Rectangle tank_rect = new(__instance.boundingBox.X, __instance.boundingBox.Y - __instance.boundingBox.Height - 64, width * 64, height * 64);
        tank_rect.X += 4;
        tank_rect.Width -= 8;
        if (__instance.QualifiedItemId == "(F)CCFishTank")
        {
            tank_rect.X += 24;
            tank_rect.Width -= 76;
        }
        tank_rect.Height -= 28;
        tank_rect.Y += 64;
        tank_rect.Height -= 64;
        __result =  tank_rect;
    }
}