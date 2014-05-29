using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.rackham.ApkHandler.Dex
{
    internal static class Constants
    {
        /// <summary>The constant array/string DEX_FILE_MAGIC is the list of bytes that must
        /// appear at the beginning of a .dex file in order for it to be recognized as such.
        /// The value intentionally contains a newline ("\n" or 0x0a) and a null byte ("\0"
        /// or 0x00) in order to help in the detection of certain forms of corruption. The
        /// value also encodes a format version number as three decimal digits, which is
        /// expected to increase monotonically over time as the format evolves.</summary>
        /// <remarks>Note: At least a couple earlier versions of the format have been used
        /// in widely-available public software releases. For example, version 009 was used
        /// for the M3 releases of the Android platform (November–December 2007), and version
        /// 013 was used for the M5 releases of the Android platform (February–March 2008).
        /// In several respects, these earlier versions of the format differ significantly
        /// from the version described in this document.</remarks>
        internal static readonly byte[] FileMagic = ASCIIEncoding.ASCII.GetBytes("dex\n035\0");

        /// <summary>The constant ENDIAN_CONSTANT is used to indicate the endianness of the
        /// file in which it is found. Although the standard .dex format is little-endian,
        /// implementations may choose to perform byte-swapping. Should an implementation come
        /// across a header whose endian_tag is REVERSE_ENDIAN_CONSTANT instead of ENDIAN_CONSTANT,
        /// it would know that the file has been byte-swapped from the expected form.</summary>
        internal const uint Endian = 0x12345678;
        internal const uint ReverseEndian = 0x78563412;

        /// <summary>The constant NO_INDEX is used to indicate that an index value is absent.
        /// Note: This value isn't defined to be 0, because that is in fact typically a valid
        /// index. Also Note: The chosen value for NO_INDEX is representable as a single byte
        /// in the uleb128p1 encoding.</summary>
        internal const uint NoIndex = 0xffffffff;    // == -1 if treated as a signed int

    }
}
