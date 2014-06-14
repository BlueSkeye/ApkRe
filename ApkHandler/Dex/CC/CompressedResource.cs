using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace com.rackham.ApkHandler.Dex.CC
{
    internal class CompressedResource
    {
        #region CONSTRUCTORS
        private CompressedResource()
        {
            return;
        }
        #endregion

        #region METHODS
        internal static CompressedResource LoadFrom(FileInfo from)
        {
            byte[] buffer;
            using (FileStream input = File.Open(from.FullName, FileMode.Open, FileAccess.Read)) {
                buffer = new byte[input.Length];
                if (buffer.Length != input.Read(buffer, 0, buffer.Length)) {
                    throw new ApplicationException("Can't load file : " + from.FullName);
                }
            }
            CompressedResource result = new CompressedResource();
            int offset = 0;
            result._header = (TableHeader)ResourceChunkHeader.Create(buffer, ref offset);
            result.Strings = (StringPool)ResourceChunkHeader.Create(buffer, ref offset);
            result.Packages = new List<Package>();
            for (int packageIndex = 0; packageIndex < result._header.PackagesCount; packageIndex++) {
                ResourceChunkHeader chunkHeader = ResourceChunkHeader.Create(buffer, ref offset);
                Package package = chunkHeader as Package;
                if (null == package) { throw new CompressedFormatException("Package missing"); }
                result.Packages.Add(package);
            }
            return result;
        }
        #endregion

        #region PROPERTIES
        internal StringPool Strings { get; private set; }

        internal List<Package> Packages { get; private set; }
        #endregion

        #region FIELDS
        private TableHeader _header;
        #endregion
    }
}
