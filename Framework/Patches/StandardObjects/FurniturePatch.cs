using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using System.Collections.Generic;

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
            Patch(false, nameof(Furniture.GetSeatPositions), nameof(GetSeatPositionsPrefix), [typeof(bool)]);
        }

        // Prevent picking up locked furniture
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

        // Correct chair positions
        private static bool GetSeatPositionsPrefix(Furniture __instance, ref List<Vector2> __result, bool ignore_offsets = false)
        {
            if (!ModEntry.modConfig.EnableMod)
                return true;

            Vector2 rectTileLocation = new Vector2((float)__instance.boundingBox.X / 64f, (float)__instance.boundingBox.Y / 64f);
            List<Vector2> seat_positions = new List<Vector2>();
            if (__instance.QualifiedItemId.Equals("(F)UprightPiano") || __instance.QualifiedItemId.Equals("(F)DarkPiano"))
            {
                seat_positions.Add(rectTileLocation + new Vector2(1.5f, 0f));
            }
            if ((int)__instance.furniture_type.Value == 0)
            {
                seat_positions.Add(rectTileLocation);
            }
            if ((int)__instance.furniture_type.Value == 1)
            {
                for (int x = 0; x < __instance.getTilesWide(); x++)
                {
                    for (int y = 0; y < __instance.getTilesHigh(); y++)
                    {
                        seat_positions.Add(rectTileLocation + new Vector2(x, y));
                    }
                }
            }
            if ((int)__instance.furniture_type.Value == 2)
            {
                int width = __instance.defaultBoundingBox.Width / 64 - 1;
                if ((int)__instance.currentRotation.Value == 0 || (int)__instance.currentRotation.Value == 2)
                {
                    seat_positions.Add(rectTileLocation + new Vector2(0.5f, 0f));
                    for (int i = 1; i < width - 1; i++)
                    {
                        seat_positions.Add(rectTileLocation + new Vector2((float)i + 0.5f, 0f));
                    }
                    seat_positions.Add(rectTileLocation + new Vector2((float)(width - 1) + 0.5f, 0f));
                }
                else if ((int)__instance.currentRotation.Value == 1)
                {
                    for (int j = 0; j < width; j++)
                    {
                        seat_positions.Add(rectTileLocation + new Vector2(1f, j));
                    }
                }
                else
                {
                    for (int k = 0; k < width; k++)
                    {
                        seat_positions.Add(rectTileLocation + new Vector2(0f, k));
                    }
                }
            }
            if ((int)__instance.furniture_type.Value == 3)
            {
                if ((int)__instance.currentRotation.Value == 0 || (int)__instance.currentRotation.Value == 2)
                {
                    seat_positions.Add(rectTileLocation + new Vector2(0.5f, 0f));
                }
                else if ((int)__instance.currentRotation.Value == 1)
                {
                    seat_positions.Add(rectTileLocation + new Vector2(1f, 0f));
                }
                else
                {
                    seat_positions.Add(rectTileLocation + new Vector2(0f, 0f));
                }
            }
            __result = seat_positions;
            return false;
        }
    }
}
