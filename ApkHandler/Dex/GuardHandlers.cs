using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.rackham.ApkHandler.Dex
{
    internal class GuardHandlers
    {
        #region PROPERTIES
        internal uint CatchAllHandlerAddress { get; set; }

        internal List<KeyValuePair<string, uint>> CatchClauses { get; private set; }
        #endregion

        #region METHODS
        internal void AddCatchClause(string caughtType, uint handlerAddress)
        {
            if (null == CatchClauses) { CatchClauses = new List<KeyValuePair<string, uint>>(); }
            CatchClauses.Add(new KeyValuePair<string, uint>(caughtType, handlerAddress));
            return;
        }
        #endregion
    }
}
