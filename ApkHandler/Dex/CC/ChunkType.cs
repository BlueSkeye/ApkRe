using System;

namespace com.rackham.ApkHandler.Dex.CC
{
    internal enum ChunkType : ushort
    {
        Null = 0x0000,
        StringPool = 0x0001,
        Table = 0x0002,
        Xml = 0x0003,
        
        // Chunk types in RES_XML_TYPE
        XmlFirstChunk = 0x0100,
        XmlNamespaceStart = 0x0100,
        XmlNamespaceEnd = 0x0101,
        XmlElementStart = 0x0102,
        XmlElementEnd = 0x0103,
        XmlCData = 0x0104,
        XmlLastChunk = 0x017f,
        
        // This contains a uint32_t array mapping strings in the string
        // pool back to resource identifiers.  It is optional.
        XmlResourceMap = 0x0180,
        
        // Chunk types in RES_TABLE_TYPE
        TablePackage = 0x0200,
        TableType = 0x0201,
        TableTypeSpec = 0x0202
    }
}
