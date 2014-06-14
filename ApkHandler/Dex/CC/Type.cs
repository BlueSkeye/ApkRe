using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkHandler.Dex.CC
{
    internal class Type : ResourceChunkHeader
    {
        #region CONSTRUCTORS
        internal Type(byte[] buffer, ref int offset, StringPool keyStrings)
            : base(buffer, ref offset)
        {
            int baseOffset = (int)(offset - ResourceChunkHeader.ChunkHeaderSize);
            Id = buffer[offset++];
            // Skip null bytes.
            offset += 3;
            // Number of uint32_t entry indices that follow.
            uint entryCount = Helpers.ReadUInt32(buffer, ref offset);
            // Offset from header where ResTable_entry data starts.
            uint entriesStart = Helpers.ReadUInt32(buffer, ref offset);
            Configuration = new ResourceTableConfiguration(buffer, ref offset);
            uint[] valuesOffset = new uint[(int)entryCount];
            for (int index = 0; index < entryCount; index++) {
                uint candidateOffset = Helpers.ReadUInt32(buffer, ref offset);
                valuesOffset[index] = (uint.MaxValue == candidateOffset)
                    ? uint.MaxValue
                    : (uint)(baseOffset + entriesStart + candidateOffset);
            }
            Resources = new Resource[entryCount];
            for(int index = 0; index < entryCount; index++) {
                if (uint.MaxValue == valuesOffset[index]) { continue; }
                offset = (int)valuesOffset[index];
                Resources[index] = new Resource(buffer, ref offset, keyStrings);
            }
            offset = (int)(baseOffset + base.Size);
            return;
        }
        #endregion

        #region PROPERTIES
        // Configuration this collection of entries is designed for.
        internal ResourceTableConfiguration Configuration { get; private set; }

        // The type identifier this chunk is holding. Type IDs start at 1 (corresponding
        // to the value of the type bits in a resource identifier).  0 is invalid.
        internal byte Id { get; private set; }

        internal Resource[] Resources { get; private set; }
        #endregion
    }
}
