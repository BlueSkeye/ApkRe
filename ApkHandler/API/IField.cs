using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.rackham.ApkHandler.API
{
    public interface IField
    {
        AccessFlags AccessFlags { get; }

        IClass Class { get; }

        string Name { get; }
    }
}
