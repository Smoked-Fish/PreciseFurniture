using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using System;
using System.Reflection;

namespace PreciseFurniture
{
    internal class ModConfig
    {
        [DefaultValue(true)]
        public bool EnableMod { get; set; }

        [DefaultValue(true)]
        public bool MoveCursor { get; set; }

        [DefaultValue(true)]
        public bool BlacklistPreventsPickup { get; set; }

        [DefaultValue(SButton.Up)]
        public KeybindList RaiseButton { get; set; }

        [DefaultValue(SButton.Down)]
        public KeybindList LowerButton { get; set; } 

        [DefaultValue(SButton.Left)]
        public KeybindList LeftButton { get; set; }

        [DefaultValue(SButton.Right)]
        public KeybindList RightButton { get; set; }

        [DefaultValue(SButton.MouseRight)]
        public KeybindList BlacklistKey { get; set; }

        [DefaultValue(SButton.LeftAlt)]
        public KeybindList ModKey { get; set; }

        [DefaultValue(5)]
        public int ModSpeed { get; set; }

        [DefaultValue(10)]
        public int MoveSpeed { get; set; }

        public ModConfig()
        {
            InitializeDefaultConfig();
        }

        public void InitializeDefaultConfig()
        {
            PropertyInfo[] properties = GetType().GetProperties();

            foreach (PropertyInfo property in properties)
            {
                DefaultValueAttribute defaultValueAttribute = (DefaultValueAttribute)property.GetCustomAttribute(typeof(DefaultValueAttribute));
                if (defaultValueAttribute != null)
                {
                    // Get the default value specified in the attribute
                    object defaultValue = defaultValueAttribute.Value;

                    // If the property type is KeybindList and the default value is SButton, convert it to KeybindList
                    if (property.PropertyType == typeof(KeybindList) && defaultValue is SButton)
                    {
                        defaultValue = new KeybindList((SButton)defaultValue);
                    }

                    // Set the property value to its default value
                    property.SetValue(this, defaultValue);
                }
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    internal class DefaultValueAttribute : Attribute
    {
        public object Value { get; }

        public DefaultValueAttribute(object value)
        {
            Value = value;
        }
    }
}
