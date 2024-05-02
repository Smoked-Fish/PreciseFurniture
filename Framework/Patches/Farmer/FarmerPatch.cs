using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Objects;
using StardewValley;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Object = StardewValley.Object;
using StardewValley.Util;
using System.Reflection;

namespace PreciseFurniture.Framework.Patches.Farmers
{
    internal class FarmerPatch : PatchTemplate
    {
        internal FarmerPatch(Harmony harmony) : base(harmony, typeof(Farmer)) { }
        internal void Apply()
        {
            Patch(PatchType.Prefix, nameof(Farmer.StopSitting), nameof(StopSittingPrefix), [typeof(bool)]);
        }

        private static bool StopSittingPrefix(Farmer __instance, bool animate = true)
        {
            if (!ModEntry.modConfig.EnableMod)
                return true;

            if (__instance.sittingFurniture == null)
            {
                return false;
            }
            ISittable furniture = __instance.sittingFurniture;
            if (!animate)
            {
                __instance.mapChairSitPosition.Value = new Vector2(-1f, -1f);
                furniture.RemoveSittingFarmer(__instance);
            }
            bool furniture_is_in_this_location = false;
            bool location_found = false;
            Vector2 old_position = __instance.Position;
            if (furniture.IsSeatHere(__instance.currentLocation))
            {
                furniture_is_in_this_location = true;
                List<Vector2> exit_positions = new List<Vector2>();
                Vector2 sit_position = new Vector2(furniture.GetSeatBounds().Left, furniture.GetSeatBounds().Top);
                if (furniture.IsSittingHere(__instance))
                {
                    sit_position = furniture.GetSittingPosition(__instance, ignore_offsets: true).Value;
                }
                if (furniture.GetSittingDirection() == 2)
                {
                    exit_positions.Add(sit_position + new Vector2(0f, 1f));
                    __instance.SortSeatExitPositions(exit_positions, sit_position + new Vector2(1f, 0f), sit_position + new Vector2(-1f, 0f), sit_position + new Vector2(0f, -1f));
                }
                else if (furniture.GetSittingDirection() == 1)
                {
                    exit_positions.Add(sit_position + new Vector2(1f, 0f));
                    __instance.SortSeatExitPositions(exit_positions, sit_position + new Vector2(0f, -1f), sit_position + new Vector2(0f, 1f), sit_position + new Vector2(-1f, 0f));
                }
                else if (furniture.GetSittingDirection() == 3)
                {
                    exit_positions.Add(sit_position + new Vector2(-1f, 0f));
                    __instance.SortSeatExitPositions(exit_positions, sit_position + new Vector2(0f, 1f), sit_position + new Vector2(0f, -1f), sit_position + new Vector2(1f, 0f));
                }
                else if (furniture.GetSittingDirection() == 0)
                {
                    exit_positions.Add(sit_position + new Vector2(0f, -1f));
                    __instance.SortSeatExitPositions(exit_positions, sit_position + new Vector2(-1f, 0f), sit_position + new Vector2(1f, 0f), sit_position + new Vector2(0f, 1f));
                }
                Rectangle bounds2 = furniture.GetSeatBounds();
                bounds2.Inflate(1, 1);
                foreach (Vector2 v in Utility.getBorderOfThisRectangle(bounds2))
                {
                    exit_positions.Add(v);
                }
                foreach (Vector2 exit_position in exit_positions)
                {
                    __instance.setTileLocation(exit_position);
                    Rectangle boundingBox = __instance.GetBoundingBox();
                    __instance.Position = old_position;
                    Object tile_object = __instance.currentLocation.getObjectAtTile((int)exit_position.X, (int)exit_position.Y, ignorePassables: true);
                    if (!__instance.currentLocation.isCollidingPosition(boundingBox, Game1.viewport, isFarmer: true, 0, glider: false, __instance) /*&& (tile_object == null || tile_object.isPassable())*/)
                    {
                        if (!(tile_object == null || tile_object.isPassable()))
                        {
                            Rectangle bounds = furniture.GetSeatBounds();
                            bounds.X *= 64;
                            bounds.Y *= 64;
                            bounds.Width *= 64;
                            bounds.Height *= 64;
                            BoundingBoxGroup temporaryPassableTiles = (BoundingBoxGroup)typeof(Farmer).GetField("temporaryPassableTiles", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
                            temporaryPassableTiles.Add(bounds);
                        }

                        if (animate)
                        {
                            __instance.playNearbySoundAll("coin");
                            __instance.synchronizedJump(4f);
                            __instance.LerpPosition(sit_position * 64f, exit_position * 64f, 0.15f);
                        }
                        location_found = true;
                        break;
                    }
                }
            }
            if (!location_found)
            {
                if (animate)
                {
                    __instance.playNearbySoundAll("coin");
                }
                __instance.Position = old_position;
                if (furniture_is_in_this_location)
                {
                    Rectangle bounds = furniture.GetSeatBounds();
                    bounds.X *= 64;
                    bounds.Y *= 64;
                    bounds.Width *= 64;
                    bounds.Height *= 64;
                    BoundingBoxGroup temporaryPassableTiles = (BoundingBoxGroup)typeof(Farmer).GetField("temporaryPassableTiles", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
                    temporaryPassableTiles.Add(bounds);
                }
            }
            if (!animate)
            {
                __instance.sittingFurniture = null;
                __instance.isSitting.Value = false;
                __instance.Halt();
                __instance.showNotCarrying();
            }
            else
            {
                __instance.isStopSitting = true;
            }
            Game1.haltAfterCheck = false;
            __instance.yOffset = 0f;
            __instance.xOffset = 0f;
            return false;
        } 
    }
}
