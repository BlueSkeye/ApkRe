using System;

namespace com.rackham.ApkJava
{
    public enum AnnotationVisibility
    {
        // intended only to be visible at build time (e.g., during compilation
        // of other code)
        Build = 0,
        // intended to visible at runtime
        Runtime = 1,
        // intended to visible at runtime, but only to the underlying system
        // (and not to regular user code)
        System = 2,
    }
}
