using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.rackham.ApkHandler.API
{
    public interface IPrototype
    {
        List<string> ParametersType { get; }

        string ReturnType { get; }
    }
}
