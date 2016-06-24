using System.Collections.Generic;

namespace com.rackham.ApkJava
{
    public class GuardHandlers
    {
        #region PROPERTIES
        public uint CatchAllHandlerAddress { get; set; }

        internal List<KeyValuePair<string, uint>> CatchClauses { get; private set; }
        #endregion

        #region METHODS
        public void AddCatchClause(string caughtType, uint handlerAddress)
        {
            if (null == CatchClauses) { CatchClauses = new List<KeyValuePair<string, uint>>(); }
            CatchClauses.Add(new KeyValuePair<string, uint>(caughtType, handlerAddress));
            return;
        }
        #endregion
    }
}
