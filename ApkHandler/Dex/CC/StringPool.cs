using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkHandler.Dex.CC
{
    internal class StringPool : ResourceChunkHeader
    {
        #region CONSTRUCTORS
        internal StringPool(byte[] buffer, ref int offset)
            : base(buffer, ref offset)
        {
            if (ChunkType.StringPool != base.Type) {
                throw new ApkFormatException("String pool was expected.");
            }
            // Number of strings in this pool (number of uint32_t indices that follow in the data).
            int stringsCount = (int)Helpers.ReadUInt32(buffer, ref offset);
            // Number of style span arrays in the pool (number of uint32_t indices
            // follow the string indices).
            int stylesCount = (int)Helpers.ReadUInt32(buffer, ref offset);
            Flags poolFlags = (Flags)Helpers.ReadUInt32(buffer, ref offset);
            // Index from header of the string data.
            uint stringsStart = Helpers.ReadUInt32(buffer, ref offset);
            // Unused - Index from header of the style data.
            uint stylesStart = Helpers.ReadUInt32(buffer, ref offset);
            // We reached the end of the string pool header. We can now retrocompute
            // the initial value of the offset as it was provided to constructor.
            int poolBaseOffset = offset - base.HeaderSize;

            _strings = new string[stringsCount];
            bool utf8 = (0 != (poolFlags & Flags.Utf8));
            for (int index = 0; index < stringsCount; index++) {
                int stringOffset = (int)Helpers.ReadUInt32(buffer, ref offset);
                _strings[index] = ReadString(buffer, (int)(poolBaseOffset + stringsStart + stringOffset), utf8);
            }
            if (0 < stylesCount) {
                _styles = new List<StyleSpan>[stylesCount];
                for (int index = 0; index < stylesCount; index++) {
                    int spanOffset = (int)Helpers.ReadUInt32(buffer, ref offset);
                    List<StyleSpan> spans = new List<StyleSpan>();
                    spanOffset += (int)(poolBaseOffset + stylesStart);
                    while (0xFFFFFFFF != Helpers.PeekUInt32(buffer, spanOffset)) {
                        spans.Add(new StyleSpan(buffer, ref spanOffset, this));
                    }
                    _styles[index] = spans;
                }
            }
            // Adjust offset to denote we consumed the whole pool.
            offset = (int)(poolBaseOffset + base.Size);
            return;
        }
        #endregion

        #region PROPERTIES
        internal string this[uint index]
        {
            get { return _strings[(int)index]; }
        }
        #endregion

        #region METHODS
        internal string GetReferencedString(byte[] buffer, ref int offset, bool nullable = false)
        {
            uint reference = Helpers.ReadUInt32(buffer, ref offset);
            if (nullable && (uint.MaxValue == reference)) { return null; }
            return this[reference];
        }

        /// <summary>Read a string from the pool starting at given offset from
        /// the given buffer.</summary>
        /// <param name="buffer">Buffer to extract from</param>
        /// <param name="offset">Initial offset.</param>
        /// <param name="utf8">true if the string is UTF8 encoded, otherwise is
        /// an UTF16 string.</param>
        /// <returns></returns>
        private static string ReadString(byte[] buffer, int offset, bool utf8)
        {
            int bytesCount;
            Encoding encoding;
            if (utf8) {
                // Skip characters count.
                if (0 != (0x80 & buffer[offset++])) { offset += 1; }
                bytesCount = buffer[offset++];
                if (0 != (0x80 & bytesCount)) {
                    bytesCount = ((bytesCount & 0x7F) << 8) + buffer[offset++];
                }
                encoding = UTF8Encoding.UTF8;
            }
            else {
                bytesCount = (int)Helpers.ReadUInt16(buffer, ref offset);
                if (0 != (0x8000 & bytesCount)) {
                    bytesCount = ((bytesCount & 0x7FFF) << 16) +
                        (int)Helpers.ReadUInt16(buffer, ref offset);
                }
                bytesCount *= 2;
                encoding = UnicodeEncoding.Unicode;
            }
            try { return encoding.GetString(buffer, offset, bytesCount); }
            catch (Exception e) { throw; }
        }
        #endregion

        #region FIELDS
        private string[] _strings;
        private List<StyleSpan>[] _styles;
        #endregion

        #region INNER CLASSES
        // Flags.
        [Flags()]
        internal enum Flags : uint
        {
            // If set, the string index is sorted by the string values (based
            // on strcmp16()).
            Sorted = 1,

            // String pool is encoded in UTF-8
            Utf8 = 256,
        };
        #endregion
    }
}
