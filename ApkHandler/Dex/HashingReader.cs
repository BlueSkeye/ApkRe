using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace com.rackham.ApkHandler.Dex
{
    internal class HashingReader
    {
        #region CONSTRUCTORS
        internal HashingReader(ChecksumReader reader)
        {
            _reader = reader;
            _hasher = (SHA1)SHA1.Create();
            return;
        }
        #endregion

        #region FIELDS
        private SHA1 _hasher;
        private ChecksumReader _reader;
        #endregion
    }
}
