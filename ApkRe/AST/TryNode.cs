using System;
using System.Collections.Generic;
using System.Text;

using com.rackham.ApkRe.CFG;
using com.rackham.ApkRe.Tree;

namespace com.rackham.ApkRe.AST
{
    /// <summary>This node is an anchor point for try ... catch ... finally ... clause
    /// handling. This node first son is always a GuardedNode used for grouping the set
    /// of guarded instructions followed by zero or more CatcherNode and an optional
    /// FinalizerNode that must always be the last son.</summary>
    internal class TryNode : AstNode
    {
        #region CONSTRUCTORS
        internal TryNode(AstNode parent)
            : base(parent)
        {
            return;
        }
        #endregion

        #region METHODS
        ///// <summary>Create a catch node that will be associated with this try node
        ///// and will handle the given exception. A set of block node are to be bound
        ///// to the newly created catch node.</summary>
        ///// <param name="blocks">A collection of one or more blocks whose associated
        ///// instructions should be added to the new catch block.</param>
        ///// <param name="caughtException">An optional string that is the name of the class
        ///// of the caucght exception. A null reference denotes a catch-all block.</param>
        ///// <returns>The newly created node.</returns>
        ///// <remarks>The catch block creation order is meaningfull. Moreover once a
        ///// catch all block has been bound to the current instance no new catch block
        ///// could be associated with this instance.</remarks>
        //internal CatchNode AddCatch(ICollection<BlockNode> blocks, string caughtException = null)
        //{
        //    if (_catchAllAdded) { throw new InvalidOperationException(); }
        //    if (null == caughtException) { _catchAllAdded = true; }
        //    CatchNode result = new CatchNode(this);
        //    GetRoot().MoveLeaves(result, blocks);
        //    return result;
        //}

        /// <summary>TODO : Assert this method uselessness and remove.</summary>
        /// <param name="root"></param>
        /// <param name="wrappedNodes"></param>
        private static ICollection<NodeBase<AstNode>> BuildMoveList(NodeBase<AstNode> root,
            ICollection<NodeBase<AstNode>> wrappedNodes)
        {
            if (null == root) { throw new ArgumentNullException(); }
            if (null == wrappedNodes) { throw new ArgumentNullException(); }
            // The root node must not appear in the wrapped nodes list.
            if (wrappedNodes.Contains(root)) { throw new ArgumentException(); }
            List<NodeBase<AstNode>> result = new List<NodeBase<AstNode>>();
            List<NodeBase<AstNode>> coveredNodes = new List<NodeBase<AstNode>>();
            root.Walk(delegate(NodeBase<AstNode> scannedNode, WalkTraversal traversal, object context)
                {
                    // Ignore root node which is not expected to be in the collection.
                    if (scannedNode == root) { return WalkContinuation.Normal; }
                    switch (traversal)
                    {
                        case WalkTraversal.CurrentNode:
                        case WalkTraversal.BeforeTransit:
                            if (object.ReferenceEquals(scannedNode.Parent, root)) {
                                result.Add(scannedNode);
                            }
                            break;
                        default:
                            break;
                    }
                    try {
                        // All encountered leaf nodes MUST be in the collection
                        if (scannedNode.IsLeafNode) {
                            if (!wrappedNodes.Contains(scannedNode)) {
                                throw new ApplicationException();
                            }
                            return WalkContinuation.Normal;
                        }
                        // An intermediate node that belongs to the collection implicitly
                        // encompass all of is direct and indirect sons. Those don't need
                        // to be in the collection.
                        if (wrappedNodes.Contains(scannedNode)) { return WalkContinuation.SkipSons; }
                        return WalkContinuation.Normal;
                    }
                    finally {
                        if (!coveredNodes.Contains(scannedNode)) {
                            coveredNodes.Add(scannedNode);
                        }
                    }
                },
            WalkMode.TransitBeforeAndAfter);
            if (coveredNodes.Count != wrappedNodes.Count) {
                throw new ApplicationException();
            }
            return result;
        }

        /// <summary>Create a new try node that will encompass all of the given nodes.</summary>
        /// <param name="wrappedNodes"></param>
        /// <returns></returns>
        internal static TryNode Create(ICollection<NodeBase<AstNode>> wrappedNodes)
        {
            NodeBase<AstNode> parent = NodeBase<AstNode>.FindNearestParent(wrappedNodes);

            if (null == parent) { throw new REException(); }
            TryNode result = new TryNode((AstNode)parent);
            // The guarded node must always be the first son.
            new GuardedNode(result, wrappedNodes);
            return result;
        }
        #endregion

        #region FIELDS
        private bool _catchAllAdded = false;
        #endregion
    }
}
