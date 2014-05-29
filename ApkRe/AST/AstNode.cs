using System;
using System.Collections.Generic;
using System.Text;

using com.rackham.ApkHandler.API;
using com.rackham.ApkRe.ByteCode;
using com.rackham.ApkRe.CFG;
using com.rackham.ApkRe.Tree;

namespace com.rackham.ApkRe.AST
{
    /// <summary>This class is the base class for each kind of AST node.</summary>
    internal class AstNode : NodeBase<AstNode>
    {
        #region CONSTRUCTORS
        /// <summary>This constructor is reserved for creation of the root node
        /// of a method.</summary>
        /// <param name="rawCode"></param>
        internal AstNode(IMethod method)
        {
            _allNodesPerOffset = new SortedList<uint, AstNode>();
            OwningMethod = method;
            Sons = new List<AstNode>();
            return;
        }

        /// <summary>This constructor is reserved for non-leaf nodes.</summary>
        /// <param name="parent"></param>
        internal AstNode(AstNode parent)
        {
            Sons = new List<AstNode>();
            parent.AddSon(this);
            // The parent property must be set AFTER the above call.
            Parent = parent;
            return;
        }

        /// <summary>Create a new instance that will be linked to the given parent node and
        /// will become the father of each and every node from the given collection. Nodes
        /// in this collection must be current sons of the parent.</summary>
        /// <param name="parent"></param>
        /// <param name="wrappedNodes"></param>
        internal AstNode(AstNode parent, ICollection<NodeBase<AstNode>> wrappedNodes)
        {
            Parent = parent;
            // Sons list insertion is directly handed by the base class relink method.
            parent.InsertGroupingNode(this, wrappedNodes);
            return;
        }

        /// <summary>This constructor is for leaf nodes.</summary>
        /// <param name="parent">Parent node.</param>
        /// <param name="methodOffset">Method relative offset of the first byte covered by
        /// this node.</param>
        /// <param name="size">Number of bytes covered by this node.</param>
        internal AstNode(AstNode parent, uint methodOffset, uint size)
        {
            if (null == parent) { throw new ArgumentNullException(); }
            // The method relative offset MUST be set before insertion in parent sons list
            // because the comparer relies on it.
            MethodRelativeOffset = methodOffset;
            parent.AddSon(this, CompareOffsets);
            // The parent property must be set AFTER the above call.
            Parent = parent;
            RegisterNode(methodOffset);
            BlockSize = size;
            Sons = new List<AstNode>();
            return;
        }
        #endregion

        #region PROPERTIES
        internal uint BlockSize { get; private set; }

        /// <summary>Get the offset within the whole method code of the first byte that
        /// belongs to this node.</summary>
        internal uint MethodRelativeOffset { get; private set; }

        internal IMethod OwningMethod { get; private set; }
        #endregion

        #region METHODS
        internal override void AddSon(AstNode son,
            Func<NodeBase<AstNode>, NodeBase<AstNode>, int> comparer = null)
        {
            // Propagate owning method reference. This must be performed before invoking
            // base otherwise the comparer will throw an exception.
            son.OwningMethod = this.OwningMethod;
            base.AddSon(son, comparer);
            return;
        }

        internal void AssertConsistency()
        {
            if (IsRootNode)
            {
                // TODO : Debug helper to assert tree consistency.
                return;
            }
            GetRoot().AssertConsistency();
            return;
        }

        /// <summary>Make sure the parameter defined zone is covered by this node otherwise
        /// throw an exception.</summary>
        /// <param name="methodOffset">Method offset zone start.</param>
        /// <param name="size">Zone length.</param>
        internal void AssertCoveredArea(uint methodOffset, uint size)
        {
            if (!IsAreaCovered(methodOffset, size)) { throw new ArgumentOutOfRangeException(); }
            return;
        }

        /// <summary>Offset comparer method that is suitable for use with the
        /// <see cref="SortSons"/> base class method.</summary>
        /// <param name="x">First node to compare. Must not be a null reference.</param>
        /// <param name="y">Second node to compare. Must not be a null reference.</param>
        /// <returns>0 if both nodes are at the same offset, -1 if x is at a lower offset
        /// than y, 1 otherwise.</returns>
        internal static int CompareOffsets(NodeBase<AstNode> x, NodeBase<AstNode> y)
        {
            if (null == x) { throw new ArgumentNullException(); }
            if (null == y) { throw new ArgumentNullException(); }
            AstNode xNode = (AstNode)x;
            AstNode yNode = (AstNode)y;
            if (!object.ReferenceEquals(xNode.OwningMethod, yNode.OwningMethod)) {
                throw new ArgumentException();
            }
            if (xNode.MethodRelativeOffset == yNode.MethodRelativeOffset) { return 0; }
            return (xNode.MethodRelativeOffset < yNode.MethodRelativeOffset) ? -1 : 1;
        }

        public override string GetDumpData()
        {
            return string.Format("@{0:X8}({1:X1})", MethodRelativeOffset, BlockSize);
        }

        /// <summary>Retrieve the node within a tree that hold the given offset.</summary>
        /// <param name="methodOffset"></param>
        /// <returns></returns>
        internal AstNode GetMethodOffsetOwner(uint methodOffset)
        {
            if (!IsRootNode) { return GetRoot().GetMethodOffsetOwner(methodOffset); }
            // TODO : Better to implement a kind of binary search.
            foreach (AstNode candidate in _allNodesPerOffset.Values) {
                if (candidate.MethodRelativeOffset > methodOffset) { continue; }
                if ((candidate.MethodRelativeOffset + BlockSize) <= methodOffset) { continue; }
                return candidate;
            }
            throw new ApplicationException();
        }

        /// <summary>Retrieve the root node from any node in the tree.</summary>
        /// <returns></returns>
        internal AstNode GetRoot()
        {
            AstNode result = this;

            while (null != result.Parent) { result = result.Parent; }
            return result;
        }

        /// <summary>Check whether the parameter defined zone is covered by this node.</summary>
        /// <param name="methodOffset">Method offset zone start.</param>
        /// <param name="size">Zone length.</param>
        internal bool IsAreaCovered(uint methodOffset, uint size)
        {
            if (methodOffset < this.MethodRelativeOffset) { return false; }
            if ((methodOffset + size) > (this.MethodRelativeOffset + this.BlockSize)) { return false; }
            return true;
        }

        /// <summary>Check whether the parameter defined zone is covering this node.</summary>
        /// <param name="methodOffset">Method offset zone start.</param>
        /// <param name="size">Zone length.</param>
        internal bool IsAreaCovering(uint methodOffset, uint size)
        {
            if (methodOffset > this.MethodRelativeOffset) { return false; }
            if ((methodOffset + size) < (this.MethodRelativeOffset + this.BlockSize)) { return false; }
            return true;
        }

        /// <summary>Check whether this block is the one that hosts the given method offset.
        /// </summary>
        /// <param name="methodOffset"></param>
        /// <returns></returns>
        internal bool IsMethodOffsetOwner(uint methodOffset)
        {
            if (methodOffset < MethodRelativeOffset) { return false; }
            return (MethodRelativeOffset + BlockSize - 1) >= methodOffset;
        }

        /// <summary></summary>
        /// <param name="targetNode"></param>
        /// <param name="from"></param>
        /// <remarks>Not optimized albeit convenient.</remarks>
        internal void MoveLeaves(AstNode targetNode, ICollection<BlockNode> from)
        {
            foreach (BlockNode scannedBlock in from) { MoveLeaves(targetNode, scannedBlock); }
            return;
        }

        /// <summary>Move leave nodes that are within the range of the given block from
        /// their current parent node to the given target node.</summary>
        /// <param name="targetNode">Target node to become the new parent for every moved
        /// node.</param>
        /// <param name="from">A CFG block node that will provide the address range to be
        /// moved.</param>
        internal void MoveLeaves(AstNode targetNode, BlockNode from)
        {
            uint blockSize;
            uint baseOffset = from.GetBaseOffsetAndSize(out blockSize);
            MoveLeaves(targetNode, baseOffset, blockSize);
            return;
        }

        /// <summary>Move leave nodes that are in the range defined by the start offset
        /// and size to be sons of the given target node.<summary>
        /// <param name="targetNode">The target node that will receive leaf nodes.</param>
        /// <param name="firstOffset">First offset to be moved.</param>
        /// <param name="size">Total size to move.</param>
        internal void MoveLeaves(AstNode targetNode, uint firstOffset, uint size)
        {
            throw new NotImplementedException();
            //if (!this.IsRootNode) {
            //    // Make sure the invocation occurs on root node instance.
            //    this.GetRoot().MoveLeaves(targetNode, firstOffset, size);
            //    return;
            //}
            //uint lastOffset = firstOffset + size - 1;
            //foreach (InstructionAstNode movedNode in
            //    WalkLeaf<DalvikInstruction>(delegate(DalvikInstruction candidate)
            //    {
            //        return (candidate.MethodRelativeOffset >= firstOffset)
            //            && (candidate.MethodRelativeOffset < lastOffset);
            //    }))
            //{
            //    movedNode.MoveTo(targetNode);
            //}
        }

        /// <summary>Move the current node to become a son of the target node.</summary>
        /// <param name="targetNode">Target node</param>
        internal void MoveTo(AstNode targetNode)
        {
            if (null == targetNode) { throw new ArgumentNullException(); }
            // This exclusion for code simplification. Will relax constraint if needed.
            if (IsRootNode) { throw new InvalidOperationException(); }
            AstNode currentParent = Parent;
            currentParent.RemoveSon(this);
            this.Parent = null;
            targetNode.AddSon(this);
            this.Parent = targetNode;
            return;
        }

        /// <summary>Register the given node as being at given method offset. This method
        /// is for use by constructors only.</summary>
        /// <param name="methodOffset">Method offset</param>
        /// <param name="registeredNode">Optional registered node. By default the node
        /// instance this method is invoked on is the instance to be registered.</param>
        private void RegisterNode(uint methodOffset, AstNode registeredNode = null)
        {
            if (null != Parent) {
                if (null == registeredNode) { registeredNode = this; }
                Parent.RegisterNode(methodOffset, registeredNode);
                return;
            }
            if (_allNodesPerOffset.ContainsKey(methodOffset)) {
                throw new REException();
            }
            _allNodesPerOffset[methodOffset] = registeredNode;
            return;
        }

        private void RemoveSon(AstNode candidate)
        {
            if (null == candidate) { throw new ArgumentNullException(); }
            List<AstNode> sons = Sons;
            int sonsCount = sons.Count;
            for (int index = 0; index < sonsCount; index++) {
                if (!object.ReferenceEquals(sons[index], candidate)) { continue; }
                // Got it.
                if (0 < index) { candidate.LeftBrother.RightBrother = candidate.RightBrother; }
                if (index < (sonsCount - 1)) { candidate.RightBrother.LeftBrother = candidate.LeftBrother; }
                sons.RemoveAt(index);
                return;
            }
            // Not an actual son of this node.
            throw new ApplicationException();
        }

        protected void SplitNotify(AstNode old, AstNode newBefore, AstNode newNode, AstNode newAfter)
        {
            _allNodesPerOffset.Remove(old.MethodRelativeOffset);
            if (null != newBefore) { _allNodesPerOffset[newBefore.MethodRelativeOffset] = newBefore; }
            _allNodesPerOffset[newNode.MethodRelativeOffset] = newNode;
            if (null != newAfter) { _allNodesPerOffset[newAfter.MethodRelativeOffset] = newAfter; }
            return;
        }
        #endregion

        #region FIELDS
        /// <summary>This is only available at root lebel.</summary>
        private SortedList<uint, AstNode> _allNodesPerOffset;
        #endregion
    }
}
