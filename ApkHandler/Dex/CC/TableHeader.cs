using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkHandler.Dex.CC
{
    internal class TableHeader : ResourceChunkHeader
    {
        #region CONSTRUCTORS
        internal TableHeader(byte[] buffer, ref int offset)
            : base(buffer, ref offset)
        {
            PackagesCount = Helpers.ReadUInt32(buffer, ref offset);
            return;
        }
        #endregion

        #region PROPERTIES
        internal uint PackagesCount { get; private set; }
        #endregion
    }
}
