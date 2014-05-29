using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkHandler.API
{
    public interface IGuardHandler
    {
        /// <summary>Caught type or a null reference for the catch-all handler.</summary>
        string CaughtType { get; }

        /// <summary>Offset (in bytes) within the method of the associated handler.</summary>
        uint HandlerMethodOffset { get; }
    }
}
