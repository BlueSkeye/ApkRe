using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkHandler.Dex.CC
{
    internal class ResourceValue
    {
        #region CONSTRUCTORS
        internal ResourceValue(byte[] buffer, ref int offset)
        {
            ushort size = Helpers.ReadUInt16(buffer, ref offset);
            offset++;
            DataType = (ResourceValueType)buffer[offset++];
            Data = Helpers.ReadUInt32(buffer, ref offset);
            return;
        }
        #endregion

        #region PROPERTIES
        internal uint Data { get; private set; }

        internal ResourceValueType DataType { get; private set; }
        #endregion

        #region METHODS
        internal string GetStringRepresentation(StringPool stringPool, PackageResolverDelegate packageResolver)
        {
            switch (DataType)
            {
                case ResourceValueType.Boolean:
                    return (0 == Data) ? "false" : "true";
                case ResourceValueType.Decimal:
                    return Data.ToString();
                case ResourceValueType.Hexadecimal:
                    return string.Format("0x{0}", Data);
                case ResourceValueType.Null:
                    return null;
                case ResourceValueType.String:
                    return stringPool[Data];
                case ResourceValueType.Reference:
                    uint packageId = (Data & 0xFF000000) >> 24;
                    uint index = (Data & 0x00FF0000) >> 16;
                    uint entryIndex = Data & 0x0000FFFF;
                    Package package = packageResolver(packageId);
                    TypeSpecification specification = package.GetType((int)index);
                    if (!specification.IsReferenceIndexValid(entryIndex)) {
                        throw new ApkFormatException();
                    }
                    // TODO : Should be more specific when several types exist under the
                    // specification. Should filter relatively to a target configuration.
                    Resource referencedResource = null;
                    foreach (Type scannedType in specification.EnumerateTypes()) {
                        referencedResource = scannedType.Resources[(int)entryIndex];
                        if (null != referencedResource) { break; }
                    }
                    if (null == referencedResource) { throw new ApkFormatException(); }
                    return referencedResource.Name;
                default:
                    throw new CompressedFormatException(
                        "Resource value type not supported : " + DataType.ToString());
            }
        }
        #endregion
    }
}
