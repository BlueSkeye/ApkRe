using System.Collections.Generic;

namespace com.rackham.ApkJava.API
{
    public interface IPrototype
    {
        List<IJavaType> ParametersType { get; }
        IJavaType ReturnType { get; }
    }
}
