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
        internal bool Build(IEnumerable<IClass> classes)
        {
            if (null == classes) { throw new ArgumentNullException(); }
            Dictionary<string, IClass> resolved = new Dictionary<string, IClass>();
            List<IClass> pending = new List<IClass>();

            foreach(IClass existing in classes) {
                resolved.Add(existing.FullName, existing);
                pending.Add(existing.SuperClass);
            }
            while (0 < pending.Count) {
                IClass scannedClass = pending[0];
                pending.RemoveAt(0);
                string seekedName = scannedClass.FullName;
                if (resolved.ContainsKey(seekedName)) { continue; }
            }
            throw new NotImplementedException();
        }
        #endregion

        #region FIELDS
        private JavaReversingContext _context;
        #endregion
    }
}
