using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.rackham.ApkHandler.Dex.CC
{
    /// <summary>Header that appears at the front of every data chunk in a resource.
    /// Matches the ResChunk_header structure</summary>
    internal class ResourceChunkHeader
    {
        #region CONSTRUCTORS
        internal ResourceChunkHeader(byte[] buffer, ref int offset)
        {
            Type = (ChunkType)Helpers.ReadUInt16(buffer, ref offset);
            HeaderSize = Helpers.ReadUInt16(buffer, ref offset);
            Size = Helpers.ReadUInt32(buffer, ref offset);
            return;
        }
        #endregion

        #region PROPERTIES
        // Size of the chunk header (in bytes). Adding this value to
        // the address of the chunk allows you to find its associated data
        // (if any).
        internal ushort HeaderSize { get; private set; }

        // Total size of this chunk (in bytes).  This is the chunkSize plus
        // the size of any data associated with the chunk.  Adding this value
        // to the chunk allows you to completely skip its contents (including
        // any child chunks).  If this value is the same as chunkSize, there is
        // no data associated with the chunk.
        internal uint Size { get; private set; }

        // Type identifier for this chunk.  The meaning of this value depends
        // on the containing chunk.
        internal ChunkType Type { get; private set; }
        #endregion

        #region METHODS
        /// <summary>This method acts as a factory. It recognizes the next chunk type
        /// and instanciate a sub class accordingly.</summary>
        /// <param name="buffer">Buffer to acquire data from.</param>
        /// <param name="offset">Offset within buffer to start reading. On return the
        /// offset is updated to reflect bytes consumption.</param>
        /// <param name="stringPool">An optional string pool that will be used to resolve
        /// some string reference. This parameter is required when handling an XML document
        /// content, it is optional for other contents.</param>
        /// <returns>An instance of the class that handle the chunk type.</returns>
        internal static ResourceChunkHeader Create(byte[] buffer, ref int offset,
            StringPool stringPool = null)
        {
            ChunkType chunkType = (ChunkType)Helpers.PeekUInt16(buffer, offset);

            switch (chunkType) {
                case ChunkType.Null:
                    return NullChunk.Create(buffer, ref offset);
                case ChunkType.StringPool:
                    return new StringPool(buffer, ref offset);
                case ChunkType.Table:
                    return new TableHeader(buffer, ref offset);
                case ChunkType.TablePackage:
                    return new Package(buffer, ref offset);
                case ChunkType.TableType:
                    if (null == stringPool) {
                        throw new ArgumentNullException("stringPool");
                    }
                    return new Type(buffer, ref offset, stringPool);
                case ChunkType.TableTypeSpec:
                    return new TypeSpecification(buffer, ref offset);
                case ChunkType.XmlElementEnd:
                case ChunkType.XmlElementStart:
                case ChunkType.XmlNamespaceEnd:
                case ChunkType.XmlNamespaceStart:
                    if (null == stringPool) {
                        throw new ArgumentNullException("stringPool");
                    }
                    return XmlTreeItem.Create(buffer, ref offset, chunkType, stringPool);
                case ChunkType.XmlResourceMap:
                    return new XmlResourceMap(buffer, ref offset);
                default:
                    throw new NotSupportedException("Unsupported chunk type : " + chunkType.ToString());
            }
        }
        #endregion

        #region FIELDS
        internal const int ChunkHeaderSize = 8;
        #endregion
    }
}
