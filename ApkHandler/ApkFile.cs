using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using com.rackham.ApkHandler.Zip;

namespace com.rackham.ApkHandler
{
    public class ApkFile
    {

        #region PROPERTIES
        public bool IsApkSigned
        {
            get { throw new NotImplementedException(); }
        }
        #endregion

        #region METHODS
        /// <summary>Extract file content into the target directory.</summary>
        /// <param name="from">Input file</param>
        /// <param name="to">target directory</param>
        public static void Extract(FileInfo from, DirectoryInfo to)
        {
            ZipExtractor.Extract(from, to);
            return;
        }

        public void HandleCompressedContents(DirectoryInfo from, DirectoryInfo to)
        {
            throw new NotImplementedException();
        }

        public void VerifySignature()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}