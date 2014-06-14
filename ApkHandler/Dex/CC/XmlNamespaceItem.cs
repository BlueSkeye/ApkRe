using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkHandler.Dex.CC
{
    internal class XmlNamespaceItem : XmlTreeItem
    {
        #region CONSTRUCTORS
        internal XmlNamespaceItem(byte[] buffer, ref int offset, StringPool stringPool,
            bool startElement)
            : base(buffer, ref offset, stringPool)
        {
            if (startElement) { base.MarkAsStarter(); }
            Prefix = stringPool.GetReferencedString(buffer, ref offset);
            Uri = stringPool.GetReferencedString(buffer, ref offset);
            return;
        }
        #endregion

        #region PROPERTIES
        internal string Prefix { get; private set; }

        internal string Uri { get; private set; }
        #endregion

        #region METHODS
        internal override bool StartEndMatch(XmlTreeItem candidate)
        {
            XmlNamespaceItem other = candidate as XmlNamespaceItem;

            if (null == other) { return false; }
            if (other.Prefix != this.Prefix) { return false; }
            if (other.Uri != this.Uri) { return false; }
            return true;
        }
        #endregion
    }
}
