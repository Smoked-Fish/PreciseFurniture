using Common.Managers;
using Common.Util;
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
            Patch(PatchType.Postfix, nameof(Furniture.canBeRemoved), nameof(CanBeRemovedPostFix), [typeof(Farmer)]);
            Patch(PatchType.Prefix, nameof(Furniture.GetSeatPositions), nameof(GetSeatPositionsPrefix), [typeof(bool)]);
            Patch(PatchType.Postfix, nameof(Furniture.IntersectsForCollision), nameof(IntersectsForCollisionPostfix), [typeof(Rectangle)]);
        }

        // Prevent picking up locked furniture
        private static void CanBeRemovedPostFix(Furniture __instance, Farmer who, ref bool __result)
        {
            if (!ModEntry.modConfig.EnableMod)
                return;

            if (ModEntry.modConfig.BlacklistPreventsPickup && __instance.modData.ContainsKey($"{modManifest.UniqueID}/blacklisted"))
            {
                if (ModEntry.furnitureToMove == null)
                {
                    Game1.addHUDMessage(new HUDMessage(I18n.Message("PickupBlacklist"), HUDMessage.error_type) { timeLeft = HUDMessage.defaultTime });
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
                int width = (__instance.defaultBoundingBox.Width / 64) - 1;
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

        // Make furniture as passable
        private static void IntersectsForCollisionPostfix(Furniture __instance, Rectangle rect, ref bool __result)
        {
            if (!ModEntry.modConfig.EnableMod)
                return;

            if (__instance.modData.ContainsKey($"{modManifest.UniqueID}/passable"))
            {
                __result = false;
            }
        }
    }
}
