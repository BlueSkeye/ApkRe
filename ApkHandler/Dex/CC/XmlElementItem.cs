using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkHandler.Dex.CC
{
    internal class XmlElementItem : XmlTreeItem
    {
        #region CONSTRUCTORS
        internal XmlElementItem(byte[] buffer, ref int offset, StringPool stringPool,
            bool startElement)
            : base(buffer, ref offset, stringPool)
        {
            int baseOffset = offset;
            Namespace = stringPool.GetReferencedString(buffer, ref offset, true);
            Name = stringPool.GetReferencedString(buffer, ref offset, true);
            if (startElement) { base.MarkAsStarter(); }
            if (!startElement) { return; }
            // End elements don't have these values.
            ushort attributeStart = Helpers.ReadUInt16(buffer, ref offset);
            ushort attributeSize = Helpers.ReadUInt16(buffer, ref offset);
            ushort attributeCount = Helpers.ReadUInt16(buffer, ref offset);
            // The three next values are 1 based indexes in the attribute array. Their
            // value is 0 if the attribute doesn't exist.
            ushort idAttributeIndex = Helpers.ReadUInt16(buffer, ref offset);
            if (0 != idAttributeIndex) { int i = 1; }
            ushort classAttributeIndex = Helpers.ReadUInt16(buffer, ref offset);
            ushort styleAttributeIndex = Helpers.ReadUInt16(buffer, ref offset);
            // This computaion should not usually modify the current offset value.
            offset = baseOffset + attributeStart;
            List<XmlElementAttributeItem> attributes = new List<XmlElementAttributeItem>();
            for (int index = 0; index < attributeCount; index++) {
                attributes.Add(new XmlElementAttributeItem(buffer, ref offset, stringPool));
            }
            Attributes = attributes.ToArray();
            return;
        }
        #endregion

        #region PROPERTIES
        internal XmlElementAttributeItem[] Attributes { get; set; }

        /// <summary>Name of this node if it is an ELEMENT; the raw character data if
        /// this is a CDATA node.</summary>
        internal string Name { get; private set; }

        /// <summary>Full namespace of this element.</summary>
        internal string Namespace { get; private set; }
        #endregion

        #region METHODS
        internal override bool StartEndMatch(XmlTreeItem candidate)
        {
            XmlElementItem other = candidate as XmlElementItem;

            if (null == other) { return false; }
            if (other.Name != this.Name) { return false; }
            if (other.Namespace != this.Namespace) { return false; }
            return true;
        }
        #endregion

        #region INNER CLASSES
        internal class XmlElementAttributeItem
        {
            #region CONSTRUCTORS
            internal XmlElementAttributeItem(byte[] buffer, ref int offset,
                StringPool stringPool)
            {
                Namespace = stringPool.GetReferencedString(buffer, ref offset, true);
                Name = stringPool.GetReferencedString(buffer, ref offset, true);
                RawValue = stringPool.GetReferencedString(buffer, ref offset, true);
                TypedValue = new ResourceValue(buffer, ref offset);
                return;
            }
            #endregion

            #region PROPERTIES
            /// <summary>Name of this attribute.</summary>
            internal string Name { get; private set; }

            /// <summary>Namespace of this attribute.</summary>
            internal string Namespace { get; private set; }

            /// <summary>The original raw string value of this attribute.</summary>
            internal string RawValue { get; private set; }

            /// <summary>Processesd typed value of this attribute.</summary>
            internal ResourceValue TypedValue { get; private set; }
            #endregion

            #region METHODS
            internal string GetStringRepresentation(StringPool stringPool,
                PackageResolverDelegate packageResolver)
            {
                return RawValue ?? TypedValue.GetStringRepresentation(stringPool, packageResolver);
            }
            #endregion
        }
        #endregion
    }
}
