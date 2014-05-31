using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace com.rackham.ApkHandler.Zip
{
    internal class LocalFileHeader : FileHeader
    {
        #region CONSTRUCTORS
        internal LocalFileHeader(FileStream input)
            : base(input)
        {
            byte[] buffer = new byte[base.FileNameLength + base.ExtraFieldLength];
            if (buffer.Length != input.Read(buffer, 0, buffer.Length)) {
                throw new ZipFormatException("Not enough data for LFH.");
            }
            int offset = 0;
            base.ReadFileNameAndExtraField(buffer, ref offset);
            return;
        }
        #endregion

        #region PROPERTIES
        protected override byte[] ExpectedSignature
        {
            get { return (byte[])_signatureBytes.Clone(); }
        }

        protected override int MinimumFileHeaderSize
        {
            get { return 30; }
        }
        #endregion

        #region FIELDS
        private static readonly byte[] _signatureBytes = new byte[] { 0x50, 0x4B, 0x03, 0x04 };
        #endregion
    }
}
