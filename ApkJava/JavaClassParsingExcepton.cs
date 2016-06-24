using System;

namespace com.rackham.ApkJava
{
    public class JavaClassParsingExcepton : ApplicationException
    {
        internal JavaClassParsingExcepton()
        {
            return;
        }

        internal JavaClassParsingExcepton(string message)
            : base(message)
        {
            return;
        }
    }
}
