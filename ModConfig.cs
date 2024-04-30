using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace PreciseFurniture
{
    internal class ModConfig
    {
        public bool EnableMod { get; set; } = true;
        public bool MoveCursor { get; set; } = true;
        public KeybindList RaiseButton { get; set; } = new KeybindList(SButton.Up);
        public KeybindList LowerButton { get; set; } = new KeybindList(SButton.Down);
        public KeybindList LeftButton { get; set; } = new KeybindList(SButton.Left);
        public KeybindList RightButton { get; set; } = new KeybindList(SButton.Right);
        public KeybindList BlacklistKey { get; set; } = new KeybindList(SButton.MouseRight);
        public KeybindList ModKey { get; set; } = new KeybindList(SButton.LeftAlt);
        public int ModSpeed { get; set; } = 5;
        public int MoveSpeed { get; set; } = 10;
    }
}
