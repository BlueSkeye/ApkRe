using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkHandler.Dex.CC
{
    internal class Resource
    {
        #region CONSTRUCTORS
        internal Resource(byte[] buffer, ref int offset, StringPool stringPool)
        {
            // Number of bytes in this structure.
            ushort size = Helpers.ReadUInt16(buffer, ref offset);
            ResourceFlags flags = (ResourceFlags)Helpers.ReadUInt16(buffer, ref offset);
            // Reference into ResTable_package::keyStrings identifying this entry.
            Name = stringPool.GetReferencedString(buffer, ref offset);
            Value = new ResourceValue(buffer, ref offset);
            return;
        }
        #endregion

        #region PROPERTIES
        internal string Name { get; private set; }

        internal ResourceValue Value { get; private set; }
        #endregion

        #region INNER CLASSES
        internal enum ResourceFlags : ushort
        {
            // If set, this is a complex entry, holding a set of name/value
            // mappings.  It is followed by an array of ResTable_map structures.
            Complex = 1,
            // If set, this resource has been declared public, so libraries
            // are allowed to reference it.
            Public = 2,
        }
        #endregion
    }
}
