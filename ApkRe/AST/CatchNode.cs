using System;
using System.Collections.Generic;
using System.Text;

using com.rackham.ApkRe.Tree;

namespace com.rackham.ApkRe.AST
{
    internal class CatchNode : AstNode
    {
        #region CONSTRUCTORS
        internal CatchNode(TryNode parent, ICollection<NodeBase<AstNode>> moved)
            : base(parent, moved)
        {
            return;
        }
        #endregion

        #region METHODS
        internal static CatchNode Create(TryNode parent, ICollection<NodeBase<AstNode>> wrappedNodes)
        {
            return new CatchNode(parent, wrappedNodes);
        }
        #endregion
    }
}
