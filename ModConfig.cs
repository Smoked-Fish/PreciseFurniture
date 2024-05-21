using Common.Interfaces;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace PreciseFurniture
{
    internal class ModConfig : IConfigurable
    {
        public event EventHandler<ConfigChangedEventArgs> ConfigChanged;

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

        [DefaultValue(SButton.None)]
        public KeybindList PassableKey { get; set; }

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

        private void OnConfigChanged(string propertyName, object oldValue, object newValue)
        {
            ConfigChanged?.Invoke(this, new ConfigChangedEventArgs(propertyName, oldValue, newValue));
        }

        public void InitializeDefaultConfig(string category = null)
        {
            PropertyInfo[] properties = GetType().GetProperties();

            foreach (PropertyInfo property in properties)
            {
                DefaultValueAttribute defaultValueAttribute = (DefaultValueAttribute)property.GetCustomAttribute(typeof(DefaultValueAttribute));
                if (defaultValueAttribute != null)
                {
                    object defaultValue = defaultValueAttribute.Value;

                    if (property.PropertyType == typeof(KeybindList) && defaultValue is SButton)
                    {
                        defaultValue = new KeybindList((SButton)defaultValue);
                    }

                    if (category != null && defaultValueAttribute.Category != category)
                    {
                        continue;
                    }

                    OnConfigChanged(property.Name, property.GetValue(this), defaultValue);
                    property.SetValue(this, defaultValue);
                }
            }
        }

        public void SetConfig(string propertyName, object value)
        {
            PropertyInfo property = GetType().GetProperty(propertyName);
            if (property != null)
            {
                try
                {
                    object convertedValue = Convert.ChangeType(value, property.PropertyType);
                    OnConfigChanged(property.Name, property.GetValue(this), convertedValue);
                    property.SetValue(this, convertedValue);
                }
                catch (Exception ex)
                {
                    ModEntry.monitor.Log($"Error setting property '{propertyName}': {ex.Message}", LogLevel.Error);
                }
            }
            else
            {
                ModEntry.monitor.Log($"Property '{propertyName}' not found in config.", LogLevel.Error);
            }
        }

    }

    [AttributeUsage(AttributeTargets.Property)]
    internal class DefaultValueAttribute : Attribute
    {
        public object Value { get; }
        public string Category { get; }

        public DefaultValueAttribute(object value, string category = null)
        {
            Value = value;
            Category = category;
        }
    }

    internal class ConfigChangedEventArgs : EventArgs
    {
        public string ConfigName { get; }
        public object OldValue { get; }
        public object NewValue { get; }

        public ConfigChangedEventArgs(string configName, object oldValue, object newValue)
        {
            ConfigName = configName;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}