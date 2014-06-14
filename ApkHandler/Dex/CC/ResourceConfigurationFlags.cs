using System;

namespace com.rackham.ApkHandler.Dex.CC
{
    [Flags()]
    internal enum ResourceConfigurationFlags
    {
        Mcc = 0x0001,
        Mnc = 0x0002,
        Locale = 0x0004,
        Touchscreen = 0x0008,
        Keyboard = 0x0010,
        HiddenKeyboard = 0x0020,
        Navigation = 0x0040,
        Orientation = 0x0080,
        Density = 0x0100,
        ScreenSize = 0x0200,
        Version = 0x0400,
        ScreenLayout = 0x0800,
        UIMode = 0x1000,

        Public = 0x40000000,
    }
}
