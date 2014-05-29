using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace com.rackham.ApkHandler.Dex
{
    internal class ChecksumReader
    {
        internal ChecksumReader(BinaryReader reader)
        {
            _reader = reader;
            return;
        }

        private BinaryReader _reader;
    }
}
