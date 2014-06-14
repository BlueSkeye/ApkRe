using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkHandler.Dex.CC
{
    internal class NullChunk : ResourceChunkHeader
    {
        #region CONSTRUCTORS
        static NullChunk()
        {
            int offset = 0;
            byte[] buffer = new byte[] { 0x00, 0x00, 0x08, 0x00, 0x08, 0x00, 0x00, 0x00 };

            Singleton = new NullChunk(buffer, ref offset);
            return;
        }

        private NullChunk(byte[] buffer, ref int offset)
            : base(buffer, ref offset)
        {
            return;
        }
        #endregion

        #region METHODS
        internal static NullChunk Create(byte[] buffer, ref int offset)
        {
            ushort type = Helpers.ReadUInt16(buffer, ref offset);
            ushort headerSize = Helpers.ReadUInt16(buffer, ref offset);
            uint size = Helpers.ReadUInt16(buffer, ref offset);
            if ((0 != type) || (8 != headerSize) || (8 != size)) {
                throw new ApkFormatException();
            }
            return Singleton;
        }
        #endregion

        #region FIELDS
        private static readonly NullChunk Singleton;
        #endregion
    }
}
