using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkHandler.Zip
{
    internal class EndOfCentralDirectory
    {
        internal EndOfCentralDirectory(byte[] buffer, int offset)
        {
            int remainingBytes = buffer.Length - offset;
            if (remainingBytes < ZipExtractor.MinimumEOCDLength) {
                throw new ZipFormatException("Not enough data for an EOCD");
            }
            if ((0x50 != buffer[offset++])
                || (0x4B != buffer[offset++])
                || (0x05 != buffer[offset++])
                || (0x06 != buffer[offset++]))
            {
                throw new ZipFormatException("EOCD signature missing");
            }
            DiskNumber = Helpers.ReadUInt16(buffer, ref offset);
            if (0 != DiskNumber) {
                throw new ZipNotSupportedFormatException("Multi disk format.");
            }
            EOCDDiskNumber = Helpers.ReadUInt16(buffer, ref offset);
            if (EOCDDiskNumber != DiskNumber) {
                throw new ZipNotSupportedFormatException("Multi disk format.");
            }
            ThisDiskRecordsCount = Helpers.ReadUInt16(buffer, ref offset);
            TotalRecordsCount = Helpers.ReadUInt16(buffer, ref offset);
            if (TotalRecordsCount != ThisDiskRecordsCount) {
                throw new ZipNotSupportedFormatException("Multi disk format.");
            }
            CentralDirectorySize = Helpers.ReadUInt32(buffer, ref offset);
            CentralDirectoryStartOffset = Helpers.ReadUInt32(buffer, ref offset);
            ushort commentLength = Helpers.ReadUInt16(buffer, ref offset);
            remainingBytes = buffer.Length - offset;
            if (remainingBytes < commentLength) {
                throw new ZipFormatException("Not enough data for an EOCD");
            }
            Comment = UTF8Encoding.UTF8.GetString(buffer, offset, commentLength);
            return;
        }

        #region PROPERTIES
        internal uint CentralDirectorySize { get; private set; }

        internal uint CentralDirectoryStartOffset { get; private set; }

        internal string Comment { get; private set; }

        internal ushort DiskNumber { get; private set; }

        internal ushort EOCDDiskNumber { get; private set; }

        internal ushort ThisDiskRecordsCount { get; private set; }

        internal ushort TotalRecordsCount { get; private set; }
        #endregion
    }
}
