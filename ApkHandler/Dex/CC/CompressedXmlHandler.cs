using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace com.rackham.ApkHandler.Dex.CC
{
    internal static class CompressedXmlHandler
    {
        #region METHODS
        internal static XmlDocument GetDocument(FileStream from, PackageResolverDelegate packageResolver)
        {
            if (null == from) { throw new ArgumentNullException(); }
            if (!from.CanRead) { throw new ArgumentException("Readable stream required."); }
            if (0 != from.Position) { throw new ArgumentException("Ill positioned stream."); }
            byte[] buffer = new byte[(int)from.Length];

            if (buffer.Length != from.Read(buffer, 0, buffer.Length)) {
                throw new ApkFormatException();
            }
            int offset = 0;
            ResourceChunkHeader header = new ResourceChunkHeader(buffer, ref offset);
            if (ChunkType.Xml != header.Type) {
                throw new ApkFormatException("XML chunk was expected.");
            }
            XmlDocument result = new XmlDocument();
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(result.NameTable);
            StringPool stringPool = new StringPool(buffer, ref offset);
            ResourceChunkHeader scannedChunk = ResourceChunkHeader.Create(buffer, ref offset, stringPool);
            XmlResourceMap resourceMap = scannedChunk as XmlResourceMap;
            // The resource map is optional. Should we have found it we need to read one
            // more chunk.
            if (null != resourceMap) { scannedChunk = ResourceChunkHeader.Create(buffer, ref offset, stringPool); }
            XmlNamespaceItem namespaceItem = scannedChunk as XmlNamespaceItem;
            if ((null == namespaceItem) || !namespaceItem.Start) {
                throw new CompressedFormatException();
            }
            namespaceManager.AddNamespace(namespaceItem.Prefix, namespaceItem.Uri);
            Stack<XmlTreeItem> stackedItem = new Stack<XmlTreeItem>();
            stackedItem.Push(namespaceItem);
            XmlElementItem currentElementItem = null;
            XmlElement currentElement = null;
            while (offset < buffer.Length) {
                XmlTreeItem item = (XmlTreeItem)ResourceChunkHeader.Create(buffer, ref offset, stringPool);
                if (item.Start) {
                    stackedItem.Push(item);
                    namespaceItem = item as XmlNamespaceItem;
                    if (null != namespaceItem) {
                        namespaceManager.AddNamespace(namespaceItem.Prefix, namespaceItem.Uri);
                        continue;
                    }
                    currentElementItem = item as XmlElementItem;
                    if (null != currentElementItem) {
                        XmlElement newElement =
                            result.CreateElement(currentElementItem.Name, currentElementItem.Namespace);
                        if (null == currentElement) { result.AppendChild(newElement); }
                        else { currentElement.AppendChild(newElement); }
                        currentElement = newElement;
                        foreach (XmlElementItem.XmlElementAttributeItem scannedAttribute in
                            currentElementItem.Attributes)
                        {
                            XmlAttribute newAttribute =
                                result.CreateAttribute(scannedAttribute.Name, scannedAttribute.Namespace);
                            newAttribute.Value = scannedAttribute.GetStringRepresentation(stringPool, packageResolver);
                            currentElement.Attributes.Append(newAttribute);
                        }
                        continue;
                    }
                    throw new NotImplementedException();
                }
                else {
                    if (0 == stackedItem.Count) {
                        throw new CompressedFormatException("Unbalanced start/end XML elements");
                    }
                    XmlTreeItem poppedItem = stackedItem.Pop();
                    if (!item.StartEndMatch(poppedItem)) {
                        throw new CompressedFormatException("Start/end XML elements mismatch");
                    }
                    // Hem. Little bit loose.
                    if (null != currentElement) {
                        currentElement = (currentElement.ParentNode as XmlElement);
                    }
                }
                int i = 1;
            }
            return result;
        }
        #endregion
    }
}
