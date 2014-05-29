using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.rackham.ApkHandler.API
{
    /// <summary>Bitfields of these flags are used to indicate the accessibility and overall
    /// properties of classes and class members.</summary>
    [Flags()]
    public enum AccessFlags : uint
    {
        /// <summary>class: visible everywhere, field: visible everywhere,
        /// method: visible everywhere</summary>
        Public = 0x1,
        /// <summary>Only allowed on for InnerClass annotations, and must not ever be on in
        /// a class_def_item.
        /// class: only visible to defining class , field: only visible to defining class,
        /// method: only visible to defining class
        /// </summary>
        Private = 0x2,
        /// <summary>>Only allowed on for InnerClass annotations, and must not ever be on in
        /// a class_def_item.
        /// class: visible to package and subclasses, field: visible to package and subclasses
        /// method: visible to package and subclasses
        /// </summary>
        Protected = 0x4,
        /// <summary>Only allowed on for InnerClass annotations, and must not ever be on in
        /// a class_def_item.
        /// class: is not constructed with an outer this reference, field: global to defining class,
        /// method: does not take a this argument
        /// </summary>
        Static = 0x8,
        /// <summary>class: not subclassable, field : immutable after construction, method: not
        /// overridable</summary>
        Final = 0x10,
        /// <summary>method: associated lock automatically acquired around call to this method.
        /// Note: This is only valid to set when ACC_NATIVE is also set.</summary>
        Synchronized = 0x20,
        /// <summary>field: special access rules to help with thread safety.</summary>
        Volatile = 0x40,
        /// <summary>method : bridge method, added automatically by compiler as a type-safe bridge
        /// </summary>
        Bridge = 0x40,
        /// <summary>field: not to be saved by default serialization</summary>
        Transient = 0x80,
        /// <summary>method : last argument should be treated as a "rest" argument by compiler</summary>
        Varargs = 0x80,
        /// <summary>Method : implemented in native code</summary>
        Native = 0x100,
        /// <summary>class : multiply-implementable abstract class</summary>
        Interface = 0x200,
        /// <summary>class: not directly instantiable, method: unimplemented by this class</summary>
        Abstract = 0x400,
        /// <summary>method: strict rules for floating-point arithmetic</summary>
        Strict = 0x800,
        /// <summary>class : not directly defined in source code, field not directly defined in
        /// source code, method : not directly defined in source code</summary>
        Synthetic = 0x1000,
        /// <summary>class : declared as an annotation class</summary>
        Annotation = 0x2000,
        /// <summary>class : declared as an enumerated type, field : declared as an enumerated value</summary>
        Enumeration = 0x4000,
        /// <summary>method : constructor method (class or instance initializer)</summary>
        Constructor = 0x10000,
        /// <summary>method : declared synchronized. Note: This has no effect on execution (other than
        /// in reflection of this flag, per se).</summary>
        DeclaredSynchronized = 0x20000,
    }
}
