using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace com.rackham.ApkHandler.Zip
{
    internal class CentralDirectoryFileHeader : FileHeader
    {
        #region CONSTRUCTORS
        internal CentralDirectoryFileHeader(FileStream input)
            : base(input)
        {
            ushort fileCommentLength = Helpers.ReadUInt16(input);
            byte[] buffer = new byte[12 + base.FileNameLength + base.ExtraFieldLength + fileCommentLength];
            if (buffer.Length != input.Read(buffer, 0, buffer.Length)) {
                throw new ZipFormatException("Not enough data for CDFH");
            }
            int offset = 0;
            FileStartDiskNumber = Helpers.ReadUInt16(buffer, ref offset);
            InternalAttributes = Helpers.ReadUInt16(buffer, ref offset);
            ExternalAttributes = Helpers.ReadUInt32(buffer, ref offset);
            LocalFileHeaderOffset = Helpers.ReadUInt32(buffer, ref offset);
            base.ReadFileNameAndExtraField(buffer, ref offset);
            if (0 == fileCommentLength) { FileComment = string.Empty; }
            else
            {
                FileComment = UTF8Encoding.UTF8.GetString(buffer, offset, fileCommentLength);
                offset += fileCommentLength;
            }
            return;
        }
        #endregion

        #region PROPERTIES
        protected override byte[] ExpectedSignature
        {
            get { return (byte[])_signatureBytes.Clone(); }
        }

        internal uint ExternalAttributes { get; private set; }

        internal string FileComment { get; private set; }

        internal ushort FileStartDiskNumber { get; private set; }

        internal ushort InternalAttributes { get; private set; }

        internal uint LocalFileHeaderOffset { get; private set; }

        protected override int MinimumFileHeaderSize
        {
            get { return 32; }
        }

        internal ushort VersionMadeBy { get; private set; }
        #endregion

        #region METHODS
        protected override void SignatureRecognized(byte[] buffer, ref int offset)
        {
            VersionMadeBy = Helpers.ReadUInt16(buffer, ref offset);
            return;
        }
        #endregion

        #region FIELDS
        private static readonly byte[] _signatureBytes = new byte[] { 0x50, 0x4B, 0x01, 0x02 };
        #endregion
    }
}