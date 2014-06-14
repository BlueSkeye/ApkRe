using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkHandler.Dex.CC
{
    internal class Package : ResourceChunkHeader
    {
        #region CONSTRUCTORS
        internal Package(byte[] buffer, ref int offset)
            : base(buffer, ref offset)
        {
            int startOffset = offset - ResourceChunkHeader.ChunkHeaderSize;
            Id = Helpers.ReadUInt32(buffer, ref offset);
            byte[] nameBuffer = new byte[MaximumNameLength];
            int nameBytesCount = sizeof(ushort) * MaximumNameLength;
            int effectiveNameLength;
            for (effectiveNameLength = 0; effectiveNameLength < MaximumNameLength; effectiveNameLength++) {
                int unicodeCharOffset = sizeof(ushort) * effectiveNameLength;
                if ((0 == buffer[offset + unicodeCharOffset])
                    && (0 == buffer[offset + unicodeCharOffset + 1]))
                {
                    break;
                }
            }
            Name = UnicodeEncoding.Unicode.GetString(buffer, offset, effectiveNameLength * sizeof(ushort));
            offset += nameBytesCount;
            // Offset to a ResStringPool_header defining the resource
            // type symbol table.  If zero, this package is inheriting from
            // another base package (overriding specific values in it).
            uint typeStrings = Helpers.ReadUInt32(buffer, ref offset);
            // Last index into typeStrings that is for public use by others.
            uint lastPublicType = Helpers.ReadUInt32(buffer, ref offset);
            // Offset to a ResStringPool_header defining the resource
            // key symbol table.  If zero, this package is inheriting from
            // another base package (overriding specific values in it).
            uint keyStrings = Helpers.ReadUInt32(buffer, ref offset);
            // Last index into keyStrings that is for public use by others.
            uint lastPublicKey = Helpers.ReadUInt32(buffer, ref offset);

            // Go on with string pools
            offset = (int)(startOffset + typeStrings);
            ResourceChunkHeader chunk = ResourceChunkHeader.Create(buffer, ref offset);
            if (ChunkType.StringPool != chunk.Type) {
                throw new CompressedFormatException(
                    "Expecting a string pool, found a " + chunk.Type.ToString());
            }
            _typeNames = (StringPool)chunk;

            offset = (int)(startOffset + keyStrings);
            chunk = ResourceChunkHeader.Create(buffer, ref offset);
            if (ChunkType.StringPool != chunk.Type) {
                throw new CompressedFormatException(
                    "Expecting a string pool, found a " + chunk.Type.ToString());
            }
            _keyNames = (StringPool)chunk;
            int endOfPackageOffset = (int)(startOffset + base.Size);
            chunk = ResourceChunkHeader.Create(buffer, ref offset);
            _specifications = new List<TypeSpecification>();
            while (offset < endOfPackageOffset) {
                TypeSpecification specification = chunk as TypeSpecification;
                if (null == specification) {
                    throw new CompressedFormatException(
                        "Expecting a type specification, found a " + chunk.Type.ToString());
                }
                _specifications.Add(specification);
                while (offset < endOfPackageOffset) {
                    chunk = ResourceChunkHeader.Create(buffer, ref offset, _keyNames);
                    if (chunk is TypeSpecification) { break; }
                    specification.AddType((Type)chunk);
                }
            }
            return;
        }
        #endregion

        #region PROPERTIES
        // If this is a base package, its ID.  Package IDs start
        // at 1 (corresponding to the value of the package bits in a
        // resource identifier).  0 means this is not a base package.
        internal uint Id { get; private set; }

        // Actual name of this package, -terminated.
        internal string Name { get; private set; }
        #endregion

        #region METHODS
        internal TypeSpecification GetType(int id)
        {
            foreach (TypeSpecification candidate in _specifications) {
                if (candidate.Id == id) { return candidate; }
            }
            throw new ArgumentException();
        }
        #endregion

        #region FIELDS
        private const int MaximumNameLength = 128;
        private StringPool _keyNames;
        private StringPool _typeNames;
        private List<TypeSpecification> _specifications;
        #endregion
    }
}
