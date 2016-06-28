using System;
using System.Collections.Generic;

using com.rackham.ApkJava.API;

namespace com.rackham.ApkRe.AST
{
    internal class InheritanceHierarchyBuilder
    {
        #region CONSTRUCTORS
        internal InheritanceHierarchyBuilder(JavaReversingContext context)
        {
            if (null == context) { throw new ArgumentNullException(); }
            _context = context;
            return;
        }
        #endregion

        #region METHODS
        internal void Build(IEnumerable<IClass> classes)
        {
            if (null == classes) { throw new ArgumentNullException(); }
            Dictionary<string, IClass> resolved = new Dictionary<string, IClass>();
            List<IJavaType> pending = new List<IJavaType>();

            foreach(IClass existing in classes) {
                resolved.Add(existing.FullyQualifiedName, existing);
                pending.Add(existing.SuperClass);
            }
            while (0 < pending.Count) {
                IJavaType scannedClass = pending[0];
                pending.RemoveAt(0);
                string seekedName = scannedClass.FullyQualifiedName;
                if (resolved.ContainsKey(seekedName)) { continue; }
            }
            return;
        }
        #endregion

        #region FIELDS
        private JavaReversingContext _context;
        #endregion
    }
}
