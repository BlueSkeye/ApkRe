using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkHandler.Dex.CC
{
    /// <summary>Describes a particular resource configuration.</summary>
    internal class ResourceTableConfiguration
    {
        #region CONSTRUCTORS
        // \0\0 means "any".  Otherwise, en, fr, etc.
        internal string Country { get; private set; }

        internal DensityKind Density { get; private set; }

        internal byte InputFlags { get; private set; }

        internal byte InputPad0 { get; private set; }

        internal KeyboardKind Keyboard { get; private set; }

        // \0\0 means "any".  Otherwise, en, fr, etc.
        internal string Language { get; private set; }

        // Mobile country code (from SIM).  0 means "any".
        internal ushort Mcc { get; private set; }

        // Mobile network code (from SIM).  0 means "any".
        internal ushort Mnc { get; private set; }

        internal NavigationKind Navigation { get; private set; }

        internal NightUIModeKind NightUIMode { get; private set; }

        internal OrientationKind Orientation { get; private set; }

        internal ushort ScreenHeight { get; private set; }

        internal ushort ScreenHeightDp { get; private set; }

        internal ScreenLayoutKind ScreenLayout { get; private set; }

        internal ushort ScreenWidth { get; private set; }

        internal ushort ScreenWidthDp { get; private set; }

        // For now minorVersion must always be 0!!! Its meaning is currently undefined.
        internal ushort SdkMinorVersion { get; private set; }

        internal ushort SdkVersion { get; private set; }

        internal ushort SmallestScreenWithDp { get; private set; }

        internal TouchScreenKind TouchScreen { get; private set; }

        internal UIModeKind UIMode { get; private set; }

        internal WideLongScreenLayoutKind WideLongScreenLayout { get; private set; }

        internal ResourceTableConfiguration(byte[] buffer, ref int offset)
        {
            // Number of bytes in this structure.
            uint size = Helpers.ReadUInt32(buffer, ref offset);
            Mcc = Helpers.ReadUInt16(buffer, ref offset);
            Mnc = Helpers.ReadUInt16(buffer, ref offset);
            Language = new string(new char[] { (char)buffer[offset++], (char)buffer[offset++] });
            Country = new string(new char[] { (char)buffer[offset++], (char)buffer[offset++] });
            Orientation = (OrientationKind)buffer[offset++];
            TouchScreen = (TouchScreenKind)buffer[offset++];
            Density = (DensityKind)Helpers.ReadUInt16(buffer, ref offset);
            Keyboard = (KeyboardKind)buffer[offset++];
            Navigation = (NavigationKind)buffer[offset++];
            InputFlags = buffer[offset++];
            InputPad0 = buffer[offset++];
            ScreenWidth = Helpers.ReadUInt16(buffer, ref offset);
            ScreenHeight = Helpers.ReadUInt16(buffer, ref offset);
            SdkVersion = Helpers.ReadUInt16(buffer, ref offset);
            SdkMinorVersion = Helpers.ReadUInt16(buffer, ref offset);
            byte trash = buffer[offset++];
            ScreenLayout = (ScreenLayoutKind)(trash & 0x0F);
            WideLongScreenLayout = (WideLongScreenLayoutKind)((trash & 0xF0) >> 4);
            trash = buffer[offset++];
            UIMode = (UIModeKind)(trash & 0x0F);
            NightUIMode = (NightUIModeKind)((trash & 0xF0) >> 4);
            SmallestScreenWithDp = Helpers.ReadUInt16(buffer, ref offset);
            ScreenWidthDp = Helpers.ReadUInt16(buffer, ref offset);
            ScreenHeightDp = Helpers.ReadUInt16(buffer, ref offset);
            return;
        }
        #endregion

        #region INNER CLASSES
        internal enum DensityKind : ushort
        {
            Default = 0,
            Low = 120,
            Medium = 160,
            High = 240,
            None = 0xFFFF,
        }

        internal enum KeyboardKind
        {
            Any = 0,
            NoKeys,
            Qwerty,
            TwelveKeys,
        }        

        internal enum HiddenKeysKind
        {
            Any = 0,
            None,
            Yes,
            Soft,
        }

        internal enum HiddenNavigationKind
        {
            Any = 0,
            No,
            Yes,
            Shift,
        }

        internal enum NavigationKind
        {
            Any = 0,
            None,
            Dpad,
            Trackball,
            Wheel,
        }

        internal enum NightUIModeKind
        {
            Any,
            No,
            Yes,
        }

        internal enum OrientationKind
        {
            Any = 0,
            Portrait,
            Landscape,
            Square
        }

        internal enum ScreenLayoutKind
        {
            Any = 0,
            Small,
            Normal,
            Large,
            ExtraLarge,
        }

        internal enum TouchScreenKind
        {
            Any = 0,
            None,
            Stylus,
            Finger,
        }

        internal enum UIModeKind
        {
            Any,
            Normal,
            Desk,
            Car,
            Television,
        }

        internal enum WideLongScreenLayoutKind
        {
            Any = 0,
            No,
            Yes,
        }
        #endregion
    }
}
