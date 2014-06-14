using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.rackham.ApkHandler.Dex.CC
{
    /// <summary>This is the basic unit from an XML structure.</summary>
    internal abstract class XmlTreeItem : ResourceChunkHeader
    {
        #region CONSTRUCTORS
        protected XmlTreeItem(byte[] buffer, ref int offset, StringPool stringPool)
            : base(buffer, ref offset)
        {
            LineNumber = Helpers.ReadUInt32(buffer, ref offset);
            Comment = stringPool.GetReferencedString(buffer, ref offset, true);
            return;
        }
        #endregion

        #region PROPERTIES
        /// <summary>Optional XML comment that was associated with this element.</summary>
        internal string Comment { get; private set; }

        /// <summary>Line number in original source file at which this element appeared.</summary>
        internal uint LineNumber { get; private set; }

        internal bool Start { get; private set; }
        #endregion

        #region METHODS
        /// <summary>This method acts as a factory for sub-classes from the <see cref="XmlTreeItem"/>
        /// class.</summary>
        /// <param name="buffer">Buffer to get bytes from.</param>
        /// <param name="offset">Offset of the first unconsumed buffer byte. Will be updated on
        /// return to denote additional consumed bytes.</param>
        /// <param name="chunkType">The chunk type that has been detected.</param>
        /// <param name="stringPool">String pool from the compressed document.</param>
        /// <returns></returns>
        internal static XmlTreeItem Create(byte[] buffer, ref int offset, ChunkType chunkType,
            StringPool stringPool)
        {
            bool startElement = false;

            switch(chunkType) {
                case ChunkType.XmlNamespaceStart:
                    startElement = true;
                    goto case ChunkType.XmlNamespaceEnd;
                case ChunkType.XmlNamespaceEnd:
                    return new XmlNamespaceItem(buffer, ref offset, stringPool, startElement);
                case ChunkType.XmlElementStart:
                    startElement = true;
                    goto case ChunkType.XmlElementEnd;
                case ChunkType.XmlElementEnd:
                    return new XmlElementItem(buffer, ref offset, stringPool, startElement);
                default:
                    throw new ArgumentException();
            }
        }

        protected void MarkAsStarter()
        {
            this.Start = true;
            return;
        }

        internal abstract bool StartEndMatch(XmlTreeItem candidate);
        #endregion
    }
}
