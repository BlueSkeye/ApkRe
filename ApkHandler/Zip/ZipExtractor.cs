using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace com.rackham.ApkHandler.Zip
{
    /// <summary>Handle the APK zip-like file format for extraction.
    /// See : http://www.pkware.com/documents/APPNOTE/APPNOTE-6.3.2.TXT
    /// </summary>
    internal class ZipExtractor
    {
        #region METHODS
        internal static string EnsurePath(DirectoryInfo baseDirectory, string filename)
        {
            string[] filenameItems = filename.Split('/');
            string scannedPath = baseDirectory.FullName;
            for (int index = 0; index < filenameItems.Length - 1; index++) {
                scannedPath = Path.Combine(scannedPath, filenameItems[index]);
                if (!Directory.Exists(scannedPath)) { Directory.CreateDirectory(scannedPath); }
            }
            return Path.Combine(scannedPath, filenameItems[filenameItems.Length - 1]);
        }

        internal static void Extract(FileInfo from, DirectoryInfo to)
        {
            using (FileStream input = File.Open(from.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                EndOfCentralDirectory endOfCentralDirectory = LoadEndOfCentralDirectory(input);
                if (endOfCentralDirectory.CentralDirectoryStartOffset > from.Length) {
                    throw new ZipFormatException("Invalid EOCD start offset");
                }
                input.Seek(endOfCentralDirectory.CentralDirectoryStartOffset, SeekOrigin.Begin);
                for (int entryIndex = 0; entryIndex < endOfCentralDirectory.TotalRecordsCount; entryIndex++) {
                    CentralDirectoryFileHeader centralHeader = new CentralDirectoryFileHeader(input);
                    long savedPosition = input.Position;
                    string filePath;
                    try {
                        input.Seek(centralHeader.LocalFileHeaderOffset, SeekOrigin.Begin);
                        LocalFileHeader localHeader = new LocalFileHeader(input);
                        switch (localHeader.CompressionMethod) {
                            case CompressionMethod.Stored:
                                if (localHeader.CompressedSize != localHeader.UncompressedSize) {
                                    throw new ZipFormatException("Compression size mismatch.");
                                }
                                filePath = EnsurePath(to, localHeader.FileName);
                                using (FileStream output = File.Open(filePath, FileMode.Create, FileAccess.Write)) {
                                    Helpers.StreamCopy(input, output, (int)localHeader.CompressedSize);
                                }
                                break;
                            case CompressionMethod.Deflated:
                                filePath = EnsurePath(to, localHeader.FileName);
                                using (FileStream output = File.Open(filePath, FileMode.Create, FileAccess.Write)) {
                                    using (DeflateStream deflater = new DeflateStream(input, CompressionMode.Decompress, true)) {
                                        Helpers.StreamCopy(deflater, output, (int)localHeader.CompressedSize, false);
                                    }
                                }
                                break;
                            default:
                                throw new ZipNotSupportedFormatException("Compression method");
                        }
                    }
                    finally { input.Position = savedPosition; }
                }
            }
        }

        /// <summary>Extract the End Of Central Directory that is located at end of the file.
        /// </summary>
        /// <param name="input">Input file stream.</param>
        private static EndOfCentralDirectory LoadEndOfCentralDirectory(FileStream input)
        {
            if (input.Length > int.MaxValue) {
                throw new ApkFormatNotSupportedException("APK file too long");
            }
            int bufferLength = (int)Math.Min(input.Length, ushort.MaxValue);
            byte[] buffer = new byte[bufferLength];
            input.Seek(-bufferLength, SeekOrigin.End);
            int readBytesCount = input.Read(buffer, 0, buffer.Length);
            if (buffer.Length != readBytesCount) {
                throw new ApkFormatNotSupportedException();
            }
            int maxSignatureIndex = buffer.Length - MinimumEOCDLength;
            int candidateOffset = -1;
            int signatureIndex = 0;
            for(int signatureStartIndex = 0; signatureStartIndex < buffer.Length; signatureStartIndex++) {
                if (EOCDSignature[signatureIndex] != buffer[signatureStartIndex]) {
                    signatureIndex = 0;
                    continue;
                }
                if (++signatureIndex < EOCDSignatureLength) { continue; }
                // Here we are with a potential candidate. Assert the comment length matches.
                ushort  commentLength = Helpers.PeekUInt16(buffer, signatureStartIndex + 16);
                int expectedBufferLength = 
                    signatureStartIndex - EOCDSignatureLength + MinimumEOCDLength + commentLength + 1;
                if (buffer.Length != expectedBufferLength) {
                    signatureIndex = 0;
                    continue;
                }
                if (-1 != candidateOffset) {
                    throw new ApkFormatNotSupportedException("Multiple EOCD block candidates");
                }
                candidateOffset = signatureStartIndex - EOCDSignatureLength + 1;
                signatureIndex = 0;
            }
            if (0 > candidateOffset) { throw new ApkFormatException("EOCD signature not found"); }
            return new EndOfCentralDirectory(buffer, candidateOffset);
        }
        #endregion

        #region FIELDS
        private static readonly byte[] EOCDSignature = new byte[] { 0x50, 0x4B, 0x05, 0x06 };
        private static readonly int EOCDSignatureLength = EOCDSignature.Length;
        internal const int MinimumEOCDLength = 22;
        #endregion
    }
}