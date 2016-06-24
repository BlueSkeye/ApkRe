using System.Security.Cryptography;

using com.rackham.ApkHandler;
using com.rackham.ApkJava.API;

namespace com.rackham.ApkRe
{
#if DBGCFG
    public class CfgDebugAnnotation : AnnotationBase, IAnnotation
    {
        private CfgDebugAnnotation()
            : base(DebugAnnotationId, null)
        {
            return;
        }

        public static CfgDebugAnnotation Get
        {
            get { return _instance;}
        }

        public static Oid Id
        {
            get { return DebugAnnotationId; }
        }

        private static readonly Oid DebugAnnotationId =
            new Oid(Constants.DebugAnnotationId, "Debug annotation");
        private static readonly CfgDebugAnnotation _instance = new CfgDebugAnnotation();
    }
#endif
}
