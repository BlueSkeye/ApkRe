using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkHandler.Dex.CC
{
    public class CompressedFormatException : ApplicationException
    {
        internal CompressedFormatException()
        {
            return;
        }

        internal CompressedFormatException(string message)
            : base(message)
        {
            return;
        }
    }
}
