using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkHandler.Dex.CC
{
    internal enum ResourceValueType : byte
    {
        // Contains no data.
        Null = 0,
        // The 'data' holds a ResTable_ref, a reference to another resource table entry.
        Reference,
        // The 'data' holds an attribute resource identifier.
        Attribute,
        // The 'data' holds an index into the containing resource table's global value string pool.
        String,
        // The 'data' holds a single-precision floating point number.
        Float,
        // The 'data' holds a complex number encoding a dimension value, such as "100in".
        Dimension,
        // The 'data' holds a complex number encoding a fraction of a container.
        Fraction = 6,
        
        // Beginning of integer flavors...
        // The 'data' is a raw integer value of the form n..n.
        Decimal = 16,
        // The 'data' is a raw integer value of the form 0xn..n.
        Hexadecimal,
        // The 'data' is either 0 or 1, for input "false" or "true" respectively.
        Boolean,
        
        // Beginning of color integer flavors...
        // The 'data' is a raw integer value of the form #aarrggbb.
        Argb8,
        // The 'data' is a raw integer value of the form #rrggbb.
        Rgb8,
        // The 'data' is a raw integer value of the form #argb.
        Argb4,
        // The 'data' is a raw integer value of the form #rgb.
        Rgb4,
    }
}
