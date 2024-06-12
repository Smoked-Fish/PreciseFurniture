﻿using Common.Interfaces;
using Common.Utilities;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace PreciseFurniture;
public sealed class Config : IConfigurable
{
    [DefaultValue(true)]
    public bool EnableMod { get; set; }

    [DefaultValue(true)]
    public bool MoveCursor { get; set; }

    [DefaultValue(true)]
    public bool BlacklistPreventsPickup { get; set; }

    [DefaultValue(SButton.Up)]
    public KeybindList? RaiseButton { get; set; }

    [DefaultValue(SButton.Down)]
    public KeybindList? LowerButton { get; set; }

    [DefaultValue(SButton.Left)]
    public KeybindList? LeftButton { get; set; }

    [DefaultValue(SButton.Right)]
    public KeybindList? RightButton { get; set; }

    [DefaultValue(SButton.MouseRight)]
    public KeybindList? BlacklistKey { get; set; }

    [DefaultValue(SButton.None)]
    public KeybindList? PassableKey { get; set; }

    [DefaultValue(SButton.LeftAlt)]
    public KeybindList? ModKey { get; set; }

    [DefaultValue(5)]
    public int ModSpeed { get; set; }

    [DefaultValue(10)]
    public int MoveSpeed { get; set; }

    public Config()
    {
        ConfigUtility.InitializeDefaultConfig(this);
    }
}