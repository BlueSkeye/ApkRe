using System;
using System.Collections.Generic;

namespace com.rackham.ApkRe.Tree
{
    public partial class NodeBase<T>
        where T : NodeBase<T>
    {
        #region DELEGATES
        internal delegate bool NodeWalkSelectorDelegate(NodeBase<T> candidate);

        internal delegate bool NodeWalkSelectorDelegate<X>(X candidate)
            where X : NodeBase<T>;

        /// <summary>Delegates of this kind are used when walking the tree.</summary>
        /// <param name="node">Current node being walked. A null reference denotes the
        /// end of the walk. In this case the return value will be silently ignored.</param>
        /// <param name="traversal">The way we landed on the enumerated node.</param>
        /// <param name="context">An optional context object that has been provided
        /// at walk start.</param>
        /// <returns>What to do next relative to walk continuation. WARNING : when the
        /// returned value is <see cref="WalkContinuation.Terminater"/> the handler won't
        /// be invoked again with a null node.</returns>
        internal delegate WalkContinuation WalkNodeHandlerDelegate(NodeBase<T> node,
            WalkTraversal traversal, object context = null);
        #endregion

        #region CONSTRUCTORS
        protected NodeBase()
        {
            DebugId = _nextDebugId++;
            return;
        }
        #endregion

        #region PROPERTIES
        public uint DebugId { get; private set; }

        public bool IsLeafNode
        {
            get { return (null == Sons) || (0 == Sons.Count); }
        }

        public bool IsRootNode
        {
            get { return null == Parent; }
        }

        public T LeftBrother { get; protected set; }

        public T Parent { get; protected set; }

        public T RightBrother { get; protected set; }

        public List<T> Sons { get; protected set; }
        #endregion

        #region METHODS
        /// <summary>Add a new son to this node, optionally enforcing a sort order amongst
        /// son using the provided comparer.</summary>
        /// <param name="son">Son to be inserted.</param>
        /// <param name="comparer">An optional comparer that will allow for sons sorting.</param>
        internal virtual void AddSon(T son, Func<NodeBase<T>, NodeBase<T>, int> comparer = null)
        {
            if (null == son) { throw new ArgumentNullException(); }
            if (null != son.Parent) { throw new ApplicationException(); }
            if (null == Sons) { Sons = new List<T>(); }
            int insertIndex;

            if (null != comparer) {
                for (insertIndex = 0; insertIndex < Sons.Count; insertIndex++) {
                    if (0 <= comparer(Sons[insertIndex], son)) { break; }
                }
            }
            else { insertIndex = Sons.Count; }
            if (0 < insertIndex) {
                T leftBrother = Sons[insertIndex - 1];
                leftBrother.RightBrother = son;
                son.LeftBrother = leftBrother;
            }
            if (Sons.Count > insertIndex) {
                T rightBrother = Sons[insertIndex];
                rightBrother.LeftBrother = son;
                son.RightBrother = rightBrother;
                Sons.Insert(insertIndex, son);
            }
            else { Sons.Add(son); }
            son.Parent = (T)this;
            return;
        }

        /// <summary>Will dump to the console the content of the tree (or sub-tree)
        /// which root node is this instance.</summary>
        public void Dump()
        {
            this.Walk(NodeDumperHandler, WalkMode.TransitBeforeAndAfter,
                new DumpContext());
            return;
        }

        internal IEnumerable<T> EnumerateSons()
        {
            if (null != Sons) { foreach (T result in Sons) { yield return result; } }
            yield break;
        }

        /// <summary>Find the NodeBase instance that is a direct or indirect parent
        /// of all of the given descendents.</summary>
        /// <param name="descendents"></param>
        /// <returns></returns>
        internal static NodeBase<T> FindNearestParent(ICollection<NodeBase<T>> descendents)
        {
            if ((null == descendents) || (0 == descendents.Count)) { throw new ArgumentNullException(); }
            List<NodeBase<T>> candidates = null;
            foreach (NodeBase<T> scannedNode in descendents) {
                if (scannedNode.IsRootNode) { return scannedNode; }
                NodeBase<T> candidateNode = scannedNode.Parent;
                if (null == candidates) {
                    candidates = new List<NodeBase<T>>();
                    while (true) {
                        candidates.Add(candidateNode);
                        if (candidateNode.IsRootNode) { break; }
                        candidateNode = candidateNode.Parent;
                    }
                    continue;
                }
                while (true) {
                    int squeezeIndex = candidates.IndexOf(candidateNode);
                    if (-1 == squeezeIndex) {
                        // Detect cases where at least two nodes do not belong to the same tree.
                        if (candidateNode.IsRootNode) { return null; }
                        candidateNode = candidateNode.Parent;
                        continue;
                    }
                    if (0 < squeezeIndex) { candidates.RemoveRange(0, squeezeIndex); }
                    break;
                }
            }
            // There should at least remain one node in the list otherwise we would have
            // detected the case earlier.
            return candidates[0];
        }

        /// <summary>Find the root node of the tree starting from any instance of
        /// the tree.</summary>
        /// <returns>Root node.</returns>
        internal NodeBase<T> FindRoot()
        {
            NodeBase<T> result = this;

            while (!result.IsRootNode) { result = result.Parent; }
            return result;
        }

        /// <summary>More specialized classes can override this method to define what they
        /// want to be displayed when dumping a node from a tree.</summary>
        /// <returns></returns>
        public virtual string GetDumpData()
        {
            return string.Empty;
        }

        private NodeBase<T> GetLeftmostDescendent()
        {
            NodeBase<T> result = this;

            while (!result.IsLeafNode) { result = result.Sons[0]; }
            return result;
        }

        private NodeBase<T> GetRightmostDescendent()
        {
            NodeBase<T> result = this;

            while (!result.IsLeafNode) {
                List<T> sons = result.Sons;
                result = sons[sons.Count - 1];
            }
            return result;
        }

        /// <summary>Insert the node before current instance.</summary>
        /// <param name="inserted">Inserted node.</param>
        internal void InsertBefore(NodeBase<T> inserted)
        {
            if (null == inserted) { throw new ArgumentNullException(); }
            if (null != inserted.Parent) { throw new ArgumentNullException(); }
            if (null != inserted.LeftBrother) { throw new ArgumentNullException(); }
            if (null != inserted.RightBrother) { throw new ArgumentNullException(); }
            inserted.RightBrother = (T)this;
            inserted.LeftBrother = this.LeftBrother;
            this.LeftBrother = (T)inserted;
            if (null != inserted.LeftBrother) {
                inserted.LeftBrother.RightBrother = (T)inserted;
            }
            inserted.Parent = this.Parent;
            this.Parent.Sons.Insert(this.Parent.Sons.IndexOf((T)this), (T)inserted);
            return;
        }

        /// <summary>Insert the <paramref name="target"/> node as a son of this instance</summary>
        /// <param name="target">The inserted node. The <see cref="Parent"/> property of this node
        /// must already reference the current instance.</param>
        /// <param name="moved">A collection of nodes to be moved as new sons of the <paramref name="target"/>.
        /// All moved nodes must be direct children of the current instance and must be adjacent tp
        /// each other with respect to collection order.</param>
        internal void InsertGroupingNode(NodeBase<T> target, ICollection<NodeBase<T>> moved)
        {
            if (null == target) { throw new ArgumentNullException(); }
            if (!object.ReferenceEquals(this, target.Parent)) { throw new ArgumentException(); }
            // TODO : Should we allow for a target node already having some sons ?
            if (null == target.Sons) { target.Sons = new List<T>(); }
            NodeBase<T> firstMoved = null;
            int firstMovedSonIndex = -1;
            NodeBase<T> lastMoved = null;
            foreach(NodeBase<T> candidate in moved) {
                if (null == candidate) { throw new ArgumentNullException(); }
                if (!object.ReferenceEquals(this, candidate.Parent)) { throw new ArgumentException(); }
                if (null == firstMoved) {
                    firstMoved = candidate;
                    lastMoved = candidate;
                    firstMovedSonIndex = Sons.IndexOf((T)candidate);
                    if (-1 == firstMovedSonIndex) { throw new ArgumentException(); }
                }
                else {
                    if (!object.ReferenceEquals(lastMoved.RightBrother, candidate)) {
                        throw new ArgumentException();
                    }
                    lastMoved = candidate;
                }
                target.Sons.Add((T)candidate);
                candidate.Parent = (T)target;
            }
            int lastMovedSonIndex = Sons.IndexOf((T)lastMoved);
            if (-1 == lastMovedSonIndex) { throw new ArgumentException(); }
            Sons.RemoveRange(firstMovedSonIndex, lastMovedSonIndex - firstMovedSonIndex + 1);
            if (firstMovedSonIndex >= Sons.Count) { Sons.Add((T)target); }
            else { Sons.Insert(firstMovedSonIndex, (T)target); }
            target.LeftBrother = firstMoved.LeftBrother;
            firstMoved.LeftBrother = null;
            if (null != target.LeftBrother) { target.LeftBrother.RightBrother = (T)target; }
            target.RightBrother = lastMoved.RightBrother;
            lastMoved.RightBrother = null;
            if (null != target.RightBrother) { target.RightBrother.LeftBrother = (T)target; }
            return;
        }

        /// <summary>A walk handler delegate for dumping a tree.</summary>
        /// <param name="node"></param>
        /// <param name="traversal"></param>
        /// <param name="rawContext"></param>
        /// <returns></returns>
        private static WalkContinuation NodeDumperHandler(NodeBase<T> node, WalkTraversal traversal,
            object rawContext)
        {
            DumpContext context = (DumpContext)rawContext;
            int indentDelta = 0;

            switch (traversal)
            {
                case WalkTraversal.AfterTransit:
                    context.IndentSpacesCount -= 2;
                    return WalkContinuation.Normal;
                case WalkTraversal.BeforeTransit:
                    indentDelta += 2;
                    break;
                case WalkTraversal.CurrentNode:
                    break;
                default:
                    throw new ApplicationException();
            }
            int indentCount = context.IndentSpacesCount;
            for (int index = 0; index < indentCount; index++) { Console.Write(" "); }
            Console.WriteLine("[{0}#{1:X}] : {2}", node.GetType().Name, node.DebugId,
                node.GetDumpData());
            context.IndentSpacesCount += indentDelta;
            return WalkContinuation.Normal;
        }

        /// <summary>Sort the direct sons of this instance according to the result of
        /// the given comparer.</summary>
        /// <param name="comparer"></param>
        internal void SortSons(Func<NodeBase<T>, NodeBase<T>, int> comparer)
        {
            throw new NotImplementedException();
        }

        /// <summary>Provides a rather generic way to walk the tree. The 
        /// <see cref="TestTreeNode.DoTests"/> source code provides serveral samples
        /// of the expected behavior of this function depending on the parameters
        /// value.</summary>
        /// <param name="nodeHandler">A delegate that will be provded with each node
        /// in turn, according to walk mode.</param>
        /// <param name="mode">The way to walk the tree.</param>
        /// <param name="context">An optional context object that will be transmited
        /// to the <paramref name="nodeHandler"/> handler on each invocation.</param>
        /// <param name="leftToRightSons">Wether sons should be walked left to right
        /// (the default) or right to left.</param>
        /// <remarks>WARNING : Do not modify this function without checking toroughly
        /// that the <see cref="TestTreeNode.DoTests"/> doesn't detect a regression bug.
        /// </remarks>
        internal void Walk(WalkNodeHandlerDelegate nodeHandler, WalkMode mode,
            object context = null, bool leftToRightSons = true)
        {
            if (null == nodeHandler) { throw new ArgumentNullException(); }
            int sonsIndexIncrement = leftToRightSons ? 1 : -1;
            NodeBase<T> lastEnumeratedNode = null;
            NodeBase<T> nextTransitEnumeratedNode = null;
            WalkTraversal traversal = 0;
            NodeBase<T> candidate;
            bool firstNode = true;
            bool walkCompleted = false;

            switch (mode) {
                case WalkMode.FatherThenSons:
                case WalkMode.SonsThenFather:
                case WalkMode.TransitBeforeAndAfter:
                case WalkMode.FullTransit:
                    break;
                default:
                    throw new ArgumentException();
            }
            while (true) {
                List<T> sons;
                // First node
                if (firstNode) {
                    firstNode = false;
                    switch(mode) {
                        case WalkMode.FatherThenSons:
                            traversal = WalkTraversal.CurrentNode;
                            lastEnumeratedNode = this;
                            break;
                        case WalkMode.TransitBeforeAndAfter:
                        case WalkMode.FullTransit:
                            lastEnumeratedNode = this;
                            traversal = (lastEnumeratedNode.IsLeafNode)
                                ? WalkTraversal.CurrentNode
                                : WalkTraversal.BeforeTransit;
                            break;
                        case WalkMode.SonsThenFather:
                            // Fill stack with leftmost or rightmost branch depending on
                            // sons walk order.
                            lastEnumeratedNode = leftToRightSons
                                ? this.GetLeftmostDescendent()
                                : this.GetRightmostDescendent();
                            traversal = WalkTraversal.CurrentNode;
                            break;
                    }
                }
                // At this point lastEnumeratedNode must be the node to be handed of to
                // the node handler. traversal should also be accurate.
                WalkContinuation continuation = nodeHandler(lastEnumeratedNode, traversal, context);
                // End of walk required.
                if (walkCompleted || (WalkContinuation.Terminate == continuation)) { return; }
                switch (continuation) {
                    case WalkContinuation.Normal:
                        break;
                    case WalkContinuation.SkipBrothers:
                        switch (mode) {
                            case WalkMode.FatherThenSons:
                                // End of walk detection.
                                if (object.ReferenceEquals(this, lastEnumeratedNode)) {
                                    lastEnumeratedNode = null;
                                    walkCompleted = true;
                                    continue;
                                }
                                sons = lastEnumeratedNode.Parent.Sons;
                                lastEnumeratedNode = leftToRightSons
                                    ? sons[sons.Count - 1].GetRightmostDescendent()
                                    : sons[0].GetLeftmostDescendent();
                                break;
                            case WalkMode.SonsThenFather:
                                // End of walk detection.
                                if (object.ReferenceEquals(this, lastEnumeratedNode)) {
                                    lastEnumeratedNode = null;
                                    walkCompleted = true;
                                    continue;
                                }
                                sons = lastEnumeratedNode.Parent.Sons;
                                lastEnumeratedNode = leftToRightSons
                                    ? sons[sons.Count - 1]
                                    : sons[0];
                                break;
                            case WalkMode.FullTransit:
                            case WalkMode.TransitBeforeAndAfter:
                                // End of walk detection.
                                if (object.ReferenceEquals(this, lastEnumeratedNode)) {
                                    lastEnumeratedNode = null;
                                    walkCompleted = true;
                                }
                                else {
                                    // Otherwise immediately swith to after transit on last enumerated node parent.
                                    lastEnumeratedNode = lastEnumeratedNode.Parent;
                                    traversal = WalkTraversal.AfterTransit;
                                }
                                continue;
                        }
                        break;
                    case WalkContinuation.SkipSons:
                        switch (mode) {
                            case WalkMode.FatherThenSons:
                            case WalkMode.SonsThenFather:
                                if (lastEnumeratedNode.IsLeafNode) { break; }
                                lastEnumeratedNode = leftToRightSons
                                    ? lastEnumeratedNode.GetRightmostDescendent()
                                    : lastEnumeratedNode.GetLeftmostDescendent();
                                break;
                            case WalkMode.FullTransit:
                            case WalkMode.TransitBeforeAndAfter:
                                // Pretend we already performed the after ransit on
                                // last enumerated node.
                                traversal = WalkTraversal.AfterTransit;
                                break;
                        }
                        break;
                }

                // Here we fall back to a standard case that is independent of the handler
                // return value. The lastEnumeratedMode and traversal variables value should
                // be enough to define next node to enumerate according to current walk mode.
                switch (mode) {
                    case WalkMode.FatherThenSons:
                        traversal = WalkTraversal.CurrentNode;
                        if (!lastEnumeratedNode.IsLeafNode) {
                            sons = lastEnumeratedNode.Sons;
                                lastEnumeratedNode = leftToRightSons
                                ? sons[0] : sons[sons.Count - 1];
                            continue;
                        }
                        candidate = leftToRightSons
                            ? lastEnumeratedNode.RightBrother
                            : lastEnumeratedNode.LeftBrother;
                        if (null != candidate) {
                            lastEnumeratedNode = candidate;
                            continue;
                        }
                        // We are the last son. Must walk up the tree.
                        while (true) {
                            candidate = lastEnumeratedNode.Parent;
                            if (object.ReferenceEquals(this, candidate)) {
                                // Reached end of walk.
                                lastEnumeratedNode = null;
                                walkCompleted = true;
                                break;
                            }
                            // candidate has already been enumerated. Consider next brother
                            candidate = leftToRightSons
                                ? candidate.RightBrother
                                : candidate.LeftBrother;
                            if (null != candidate) {
                                // Brother found. Will be the next enumerated.
                                lastEnumeratedNode = candidate;
                                break;
                            }
                            lastEnumeratedNode = lastEnumeratedNode.Parent;
                        }
                        continue;
                    case WalkMode.SonsThenFather:
                        traversal = WalkTraversal.CurrentNode;
                        if (object.ReferenceEquals(this, lastEnumeratedNode)) {
                            // Reached end of walk.
                            lastEnumeratedNode = null;
                            walkCompleted = true;
                            continue;
                        }
                        candidate = leftToRightSons
                            ? lastEnumeratedNode.RightBrother
                            : lastEnumeratedNode.LeftBrother;
                        if (null != candidate) {
                            lastEnumeratedNode = leftToRightSons
                                ? candidate.GetLeftmostDescendent()
                                : candidate.GetRightmostDescendent();
                            continue;
                        }
                        // No more son. Enumerate father.
                        lastEnumeratedNode = lastEnumeratedNode.Parent;
                        continue;
                    case WalkMode.TransitBeforeAndAfter:
                    case WalkMode.FullTransit:
                        // The last traversal value gives us a hint on what we did previously.
                        switch (traversal) {
                            case WalkTraversal.AfterTransit:
                                // End of walk detection.
                                if (object.ReferenceEquals(this, lastEnumeratedNode)) { return; }
                                candidate = leftToRightSons
                                    ? lastEnumeratedNode.RightBrother
                                    : lastEnumeratedNode.LeftBrother;
                                lastEnumeratedNode = lastEnumeratedNode.Parent;
                                if (null != candidate) {
                                    if (WalkMode.TransitBeforeAndAfter == mode) {
                                        lastEnumeratedNode = candidate;
                                        traversal = lastEnumeratedNode.IsLeafNode
                                            ? WalkTraversal.CurrentNode
                                            : WalkTraversal.BeforeTransit;
                                    }
                                    else {
                                        nextTransitEnumeratedNode = candidate;
                                        traversal = WalkTraversal.Transit;
                                    }
                                }
                                else {
                                    nextTransitEnumeratedNode = null;
                                    traversal = WalkTraversal.AfterTransit;
                                }
                                break;
                            case WalkTraversal.BeforeTransit:
                                if (lastEnumeratedNode.IsLeafNode) { throw new ApplicationException(); }
                                sons = lastEnumeratedNode.Sons;
                                lastEnumeratedNode = leftToRightSons
                                    ? sons[0]
                                    : sons[sons.Count - 1];
                                traversal = lastEnumeratedNode.IsLeafNode
                                    ? WalkTraversal.CurrentNode
                                    : WalkTraversal.BeforeTransit;
                                break;
                            case WalkTraversal.Transit:
                                if (null == nextTransitEnumeratedNode) { throw new ApplicationException(); }
                                lastEnumeratedNode = nextTransitEnumeratedNode;
                                nextTransitEnumeratedNode = null;
                                traversal = lastEnumeratedNode.IsLeafNode
                                    ? WalkTraversal.CurrentNode
                                    : WalkTraversal.BeforeTransit;
                                break;
                            case WalkTraversal.CurrentNode:
                                if (!lastEnumeratedNode.IsLeafNode) { throw new ApplicationException(); }
                                // End of walk detection. In case we started walking directly on
                                // a leaf.
                                if (object.ReferenceEquals(this, lastEnumeratedNode)) { return; }
                                candidate = leftToRightSons
                                    ? lastEnumeratedNode.RightBrother
                                    : lastEnumeratedNode.LeftBrother;
                                lastEnumeratedNode = lastEnumeratedNode.Parent;
                                if (null != candidate) {
                                    if (WalkMode.TransitBeforeAndAfter == mode) {
                                        lastEnumeratedNode = candidate;
                                        traversal = lastEnumeratedNode.IsLeafNode
                                            ? WalkTraversal.CurrentNode
                                            : WalkTraversal.BeforeTransit;
                                    }
                                    else {
                                        nextTransitEnumeratedNode = candidate;
                                        traversal = WalkTraversal.Transit;
                                    }
                                }
                                else {
                                    nextTransitEnumeratedNode = null;
                                    traversal = WalkTraversal.AfterTransit;
                                }
                                break;
                            default:
                                throw new ApplicationException();
                        }
                        continue;
                    default:
                        throw new ApplicationException();
                }
            }
        }

        /// <summary>Provide an enumerable object that will return those leaf nodes
        /// that match an optional selector.</summary>
        /// <param name="selector">An optional selector that will filer leaf nodes on
        /// some criteria.</param>
        /// <returns>An enumerable object.</returns>
        internal IEnumerable<T> _WalkLeaf(NodeWalkSelectorDelegate selector = null)
        {
            NodeBase<T> startNode = this;
            T scannedNode = (T)this;
            Stack<T> walkStack = new Stack<T>();

            while (true) {
                if ((null != scannedNode.Sons) && (0 < scannedNode.Sons.Count)) {
                    // Not a leaf node. Walk down.
                    walkStack.Push(scannedNode);
                    scannedNode = scannedNode.Sons[0];
                    continue;
                }
                // Return node subject to selector decision.
                if ((null == selector) || selector(scannedNode)) { yield return scannedNode; }
                if (null != scannedNode.RightBrother) {
                    // Just returned node has a brother on its right. Make it
                    // the next candidate and loop because right brother is not
                    // necessarily a leaf.
                    scannedNode = scannedNode.RightBrother;
                    continue;
                }
                // Just returned node is the last son. Walk up the stack and at
                // each level consider any right brother.
                while (true) {
                    if (0 == walkStack.Count) { yield break; }
                    scannedNode = walkStack.Pop();
                    if (null != scannedNode.RightBrother) {
                        scannedNode = scannedNode.RightBrother;
                        break;
                    }
                }
            }
        }

        /// <summary>Provide an enumerable object that will return those leaf nodes
        /// that match an optional selector and at the same time are ot the
        /// <typeparamref name="T"/> type.</summary>
        /// <typeparam name="T">An AstNode derived type.</typeparam>
        /// <param name="selector">An optional selector that will filer leaf nodes on
        /// some criteria.</param>
        /// <returns>An enumerable object.</returns>
        internal IEnumerable<X> WalkLeaf<X>(NodeWalkSelectorDelegate<X> selector = null)
            where X : NodeBase<T>
        {
            foreach (NodeBase<T> candidate in _WalkLeaf(null)) {
                X castedCandidate = candidate as X;
                if (null == castedCandidate) { continue;}
                if ((null == selector) || selector(castedCandidate)) { yield return castedCandidate; }
            }
            yield break;
        }
        #endregion

        #region FIELDS
        private static uint _nextDebugId = 1;
        #endregion

        #region INNER CLASSES
        private class DumpContext
        {
            internal int IndentSpacesCount { get; set; }
        }
        #endregion
    }
}
