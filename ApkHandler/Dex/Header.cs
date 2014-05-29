using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace com.rackham.ApkHandler.Dex
{
    internal class Header
    {
        #region CONSTRUCTORS
        internal Header()
        {
            return;
        }
        #endregion

        #region PROPERTIES
        internal SizeAndOffset ClassesDefinition { get; private set; }

        internal SizeAndOffset FieldIdsDefinition { get; private set; }

        internal MapItem[] Map { get; private set; }

        internal SizeAndOffset MethodIdsDefinition { get; private set; }

        internal SizeAndOffset ProtoIdsDefinition { get; private set; }

        internal SizeAndOffset StringIdsDefinition { get; private set; }

        internal SizeAndOffset TypeIdsDefinition { get; private set; }
        #endregion

        #region METHODS
        internal void AssertOffsetInDataSection(uint candidate)
        {
            if (candidate < _datasDefinition.Offset) { throw new ParseException(); }
            if (candidate >= (_datasDefinition.Offset + _datasDefinition.Size)) { throw new ParseException(); }
            return;
        }

        /// <summary>Parse a DEX file header starting at current position of the <paramref name="reader"/>.
        /// On return the reader underlying stream position is set on the first byte to be included
        /// in the SHA1 hashing value.</summary>
        /// <param name="reader">The reader to acquire data from.</param>
        /// <returns>The header object.</returns>
        internal static Header Parse(BinaryReader reader)
        {
            Header result = new Header();
            long initialPosition = reader.BaseStream.Position;

            Helpers.Align(reader, Alignement);
            byte[] magic = new byte[Constants.FileMagic.Length];
            reader.Read(magic, 0, magic.Length);
            if (!Helpers.AreEqual(magic, Constants.FileMagic)) { throw new ParseException(); }
            result._expectedChecksum = reader.ReadUInt32();
            result._expectedHash = new byte[20];
            reader.Read(result._expectedHash, 0, result._expectedHash.Length);
            long firstHashedBytePosition = reader.BaseStream.Position;
            result._fileSize = reader.ReadUInt32();
            if (result._fileSize != (reader.BaseStream.Length - initialPosition)) {
                throw new ParseException();
            }
            // size of the header (this entire section), in bytes. This allows for at least a
            // limited amount of backwards/forwards compatibility without invalidating the format.
            if (0x70 != reader.ReadUInt32()) { throw new ParseException(); }
            uint endianess = reader.ReadUInt32();
            switch (endianess)
            {
                case Constants.Endian:
                    break;
                case Constants.ReverseEndian:
                    // TODO 
                    throw new NotSupportedException();
                default:
                    throw new ParseException();
            }
            // size of the link section, or 0 if this file isn't statically linked
            // offset from the start of the file to the link section, or 0 if link_size == 0.
            // The offset, if non-zero, should be to an offset into the link_data section. The
            // format of the data pointed at is left unspecified by this document; this header
            // field (and the previous) are left as hooks for use by runtime implementations.
            SizeAndOffset linkDefinition = new SizeAndOffset(reader);
            // offset from the start of the file to the map item, or 0 if this file has no map.
            // The offset, if non-zero, should be to an offset into the data section, and the
            // data should be in the format specified by "map_list" below.
            uint mapFileOffset = reader.ReadUInt32();
            if (0 != mapFileOffset) { result.Map = ParseMap(reader, mapFileOffset); }
            // count of strings in the string identifiers list & offset from the start of the
            // file to the string identifiers list, or 0 if string_ids_size == 0 (admittedly a
            // strange edge case). The offset, if non-zero, should be to the start of the
            // string_ids section.
            result.StringIdsDefinition = new SizeAndOffset(reader);
            // count of elements in the type identifiers list & offset from the start of the
            // file to the type identifiers list, or 0 if type_ids_size == 0 (admittedly a
            // strange edge case). The offset, if non-zero, should be to the start of the
            // type_ids section.
            result.TypeIdsDefinition = new SizeAndOffset(reader);
            // count of elements in the prototype identifiers list & offset from the start of
            // the file to the prototype identifiers list, or 0 if proto_ids_size == 0
            // (admittedly a strange edge case). The offset, if non-zero, should be to the
            // start of the proto_ids section.
            result.ProtoIdsDefinition = new SizeAndOffset(reader);
            // count of elements in the field identifiers list & offset from the start of the
            // file to the field identifiers list, or 0 if field_ids_size == 0. The offset,
            // if non-zero, should be to the start of the field_ids section.
            result.FieldIdsDefinition = new SizeAndOffset(reader);
            // count of elements in the method identifiers list & offset from the start of the
            // file to the method identifiers list, or 0 if method_ids_size == 0. The offset,
            // if non-zero, should be to the start of the method_ids section.
            result.MethodIdsDefinition = new SizeAndOffset(reader);
            // count of elements in the class definitions list & offset from the start of the
            // file to the class definitions list, or 0 if class_defs_size == 0 (admittedly a
            // strange edge case). The offset, if non-zero, should be to the start of the
            // class_defs section.
            result.ClassesDefinition = new SizeAndOffset(reader);
            // Size of data section in bytes. Must be an even multiple of sizeof(uint). &
            // offset from the start of the file to the start of the data section.
            result._datasDefinition = new SizeAndOffset(reader);
            reader.BaseStream.Position = firstHashedBytePosition;
            return result;
        }

        /// <summary></summary>
        /// <param name="reader"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        /// <remarks>This is a list of the entire contents of a file, in order. It contains
        /// some redundancy with respect to the header_item but is intended to be an easy
        /// form to use to iterate over an entire file. A given type must appear at most once
        /// in a map, but there is no restriction on what order types may appear in, other
        /// than the restrictions implied by the rest of the format (e.g., a header section
        /// must appear first, followed by a string_ids section, etc.). Additionally, the map
        /// entries must be ordered by initial offset and must not overlap.</remarks>
        private static MapItem[] ParseMap(BinaryReader reader, uint offset)
        {
            long initialPosition = reader.BaseStream.Position;
            reader.BaseStream.Position = offset;
            Helpers.Align(reader, 4);
            // size of the list, in entries
            uint itemsCount = reader.ReadUInt32();
            MapItem[] result = new MapItem[itemsCount];
            for (int index = 0; index < itemsCount; index++) {
                // type of the items
                MapItem.ItemType itemType = (MapItem.ItemType)reader.ReadUInt16();
                // An unused 2 bytes value is present.
                reader.ReadUInt16();
                // count of the number of items to be found at the indicated offset
                uint typedItemsCount = reader.ReadUInt32();
                // offset from the start of the file to the items in question
                uint typedItemsOffset = reader.ReadUInt32();

                result[index] = new MapItem(itemType, typedItemsCount, typedItemsOffset);
            }
            reader.BaseStream.Position = initialPosition;
            return result;
        }
        #endregion

        #region FIELDS
        internal const int Alignement = 4;
        private SizeAndOffset _datasDefinition;
        /// <summary>adler32 checksum of the rest of the file (everything but magic and this
        /// field); used to detect file corruption</summary>
        internal uint _expectedChecksum;
        /// <summary>SHA-1 signature (hash) of the rest of the file (everything but magic, checksum,
        /// and this field); used to uniquely identify files</summary>
        internal byte[] _expectedHash;
        /// <summary>size of the entire file (including the header), in bytes</summary>
        internal uint _fileSize;
        #endregion

        #region INNER CLASSES
        internal struct SizeAndOffset
        {
            private SizeAndOffset(uint size, uint offset)
            {
                _size = size;
                _offset = offset;
                return;
            }

            internal SizeAndOffset(BinaryReader reader)
            {
                _size = reader.ReadUInt32();
                _offset = reader.ReadUInt32();
                return;
            }

            #region PROPERTIES
            internal uint Offset
            {
                get { return _offset; }
            }

            internal uint Size
            {
                get { return _size; }
            }
            #endregion

            #region FIELDS
            private uint _offset;
            private uint _size;
            #endregion
        }
        #endregion
    }
}
