using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkHandler.Dex.CC
{
    internal class StyleSpan
    {
        #region CONSTRUCTORS
        internal StyleSpan(byte[] buffer, ref int offset, StringPool stringPool)
        {
            Name = stringPool.GetReferencedString(buffer, ref offset);
            FirstCharacter = Helpers.ReadUInt32(buffer, ref offset);
            LastCharacter = Helpers.ReadUInt32(buffer, ref offset);
            return;
        }
        #endregion

        #region PROPERTIES
        internal uint FirstCharacter { get; private set; }

        internal uint LastCharacter { get; private set; }

        internal string Name { get; private set; }
        #endregion
    }
}
