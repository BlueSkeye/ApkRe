using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkRe.AST
{
    internal class CatcherNode : AstNode
    {
        #region CONSTRUCTORS
        internal CatcherNode(TryNode parent, string caughtClass)
            : base(parent)
        {
            return;
        }
        #endregion
    }
}
