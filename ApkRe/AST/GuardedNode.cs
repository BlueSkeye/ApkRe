using System;
using System.Collections.Generic;
using System.Text;

using com.rackham.ApkRe.Tree;

namespace com.rackham.ApkRe.AST
{
    internal class GuardedNode : AstNode
    {
        #region CONSTRUCTORS
        internal GuardedNode(TryNode parent, ICollection<NodeBase<AstNode>> moved)
            : base(parent, moved)
        {
            return;
        }
        #endregion

    }
}
