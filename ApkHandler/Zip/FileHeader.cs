using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace com.rackham.ApkHandler.Zip
{
    internal abstract class FileHeader
    {
        #region CONSTRUCTORS
        protected FileHeader(FileStream input)
        {
            byte[] buffer = new byte[MinimumFileHeaderSize];
            if (buffer.Length != input.Read(buffer, 0, buffer.Length)) {
                throw new ZipFormatException("Not enough data available for file header");
            }
            int bufferOffset = 0;
            byte[] expectedSignature = ExpectedSignature;
            for (int index = 0; index < expectedSignature.Length; index++) {
                if (buffer[bufferOffset++] != expectedSignature[index]) {
                    throw new ZipFormatException("File header signature mismatch");
                }
            }
            SignatureRecognized(buffer, ref bufferOffset);
            MinimumExtractorVersion = Helpers.ReadUInt16(buffer, ref bufferOffset);
            Flags = Helpers.ReadUInt16(buffer, ref bufferOffset);
            CompressionMethod = (CompressionMethod)Helpers.ReadUInt16(buffer, ref bufferOffset);
            LastModified = Helpers.ReadUInt32(buffer, ref bufferOffset);
            CRC32 = Helpers.ReadUInt32(buffer, ref bufferOffset);
            CompressedSize = Helpers.ReadUInt32(buffer, ref bufferOffset);
            UncompressedSize = Helpers.ReadUInt32(buffer, ref bufferOffset);
            FileNameLength = Helpers.ReadUInt16(buffer, ref bufferOffset);
            ExtraFieldLength = Helpers.ReadUInt16(buffer, ref bufferOffset);
            return;
        }
        #endregion

        #region PROPERTIES
        internal uint CompressedSize { get; private set; }

        protected uint CRC32 { get; private set; }

        internal CompressionMethod CompressionMethod { get; private set; }

        protected abstract byte[] ExpectedSignature { get; }

        internal string ExtraField { get; private set; }

        protected ushort ExtraFieldLength { get; private set; }

        internal string FileName { get; private set; }

        protected ushort FileNameLength { get; private set; }

        protected ushort Flags { get; private set; }

        protected uint LastModified { get; private set; }

        protected ushort MinimumExtractorVersion { get; private set; }

        protected abstract int MinimumFileHeaderSize { get; }

        internal uint UncompressedSize { get; private set; }
        #endregion

        #region METHODS
        protected void ReadFileNameAndExtraField(byte[] buffer, ref int offset)
        {
            if (0 == FileNameLength) { FileName = string.Empty; }
            else {
                FileName = UTF8Encoding.UTF8.GetString(buffer, offset, FileNameLength);
                offset += FileNameLength;
            }
            if (0 == ExtraFieldLength) { ExtraField = string.Empty; }
            else {
                ExtraField = UTF8Encoding.UTF8.GetString(buffer, offset, ExtraFieldLength);
                offset += ExtraFieldLength;
            }
            return;
        }

        protected virtual void SignatureRecognized(byte[] buffer, ref int offset)
        {
            return;
        }
        #endregion
    }
}