using System;

namespace com.rackham.ApkHandler.Dex
{
    internal class MapItem
    {
        #region CONSTRUCTORS
        internal MapItem(ItemType type, uint count, uint offset)
        {
            Type = type;
            Count = count;
            Offset = offset;
            return;
        }
        #endregion

        #region PROPERTIES
        internal uint Count { get; private set; }

        internal uint Offset { get; private set; }

        internal ItemType Type { get; private set; }
        #endregion

        #region INNER CLASSES
        internal enum ItemType : ushort
        {
            Header = 0, // 0x70 bytes
            StringId = 1, // 0x04 bytes
            TypeId = 2, // 0x04 bytes
            PrototypeId = 3, // 0x0c bytes
            FieldId = 4, // 0x08 bytes
            MethodId = 5, // 0x08 bytes
            ClassDefinition = 6, // 0x20 bytes
            MapList = 0x1000, // 4 + (item.size * 12) bytes
            TypeList = 0x1001, // 4 + (item.size * 2) bytes
            AnnotationSetReference = 0x1002, // 4 + (item.size * 4) bytes
            AnnotationSet = 0x1003, // 4 + (item.size * 4) bytes
            ClassData = 0x2000, // implicit; must parse
            CodeItem = 0x2001, // implicit; must parse
            StringData = 0x2002, // implicit; must parse
            DebugInfo = 0x2003, // implicit; must parse
            AnnotationItem = 0x2004, // implicit; must parse
            EncodedArray = 0x2005, // implicit; must parse
            AnnotationsDirectory = 0x2006, // implicit; must parse
        }
        #endregion
    }
}
