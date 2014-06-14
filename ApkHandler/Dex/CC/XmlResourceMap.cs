using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkHandler.Dex.CC
{
    /// <summary>This element is optional. It is an array of resource identifiers.
    /// The identifier at index x is the one of any resource which name refers the
    /// xth item from the string pool associated with the compressed XML document
    /// this map is bound to.</summary>
    internal class XmlResourceMap : ResourceChunkHeader
    {
        #region CONSTRUCTORS
        internal XmlResourceMap(byte[] buffer, ref int offset)
            : base(buffer, ref offset)
        {
            int limitOffset = offset + (int)(base.Size - base.HeaderSize - 1);
            List<uint> ids = new List<uint>();
            while (offset < limitOffset) {
                ids.Add(Helpers.ReadUInt32(buffer, ref offset));
            }
            _ids = ids.ToArray();
            return;
        }
        #endregion

        #region PROPERTIES
        /// <summary>Retrieves identifier for resource at given index. If index is
        /// out of arry bounds, the return value is 0.</summary>
        /// <param name="index"></param>
        /// <returns></returns>
        internal uint this[int index]
        {
            get { return ((0 > index) || (index >= _ids.Length)) ? 0 : _ids[index]; }
        }
        #endregion

        #region FIELDS
        private uint[] _ids;
        #endregion
    }
}
