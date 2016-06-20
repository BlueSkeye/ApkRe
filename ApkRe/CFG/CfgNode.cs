using System;
using System.Collections.Generic;
using System.Text;

using com.rackham.ApkRe.AST;
using com.rackham.ApkRe.Tree;

namespace com.rackham.ApkRe.CFG
{
    internal class CfgNode : IGraphNode
    {
        #region CONSTRUCTORS
#if DBGCFG
        internal CfgNode(bool debugCfg = false)
#else
        internal CfgNode()
#endif
        {
#if DBGCFG
            lock (typeof(CfgNode)) { NodeId = NextNodeId++; }
            DebugEnabled = debugCfg;
#endif
            return;
        }
        #endregion

        #region PROPERTIES
        internal virtual string AdditionalInfo
        {
            get { return string.Empty; }
        }

#if DBGCFG
        internal bool DebugEnabled { get; set; }
#endif

        /// <summary>Check if this node is an entry point into the graph. An entry point
        /// is a node without any predecessor.</summary>
        public bool IsEntryNode
        {
            get { return (null == Predecessors) || (0 == Predecessors.Length); }
        }

        /// <summary>Check if this node is an exit node from the graph. An exit point is
        /// one without any successor.</summary>
        public bool IsExitNode
        {
            get { return (null == Successors) || (0 == Successors.Length); }
        }

#if DBGCFG
        internal uint NodeId { get; set; }
#endif

        /// <summary>Get an array of nodes that are predecessors for this one.</summary>
        public CfgNode[] Predecessors
        {
            get { return (null == _predecessors) ? null : _predecessors.ToArray(); }
        }

        IEnumerable<IGraphNode> IGraphNode.Predecessors
        {
            get
            {
                if (null == this.Predecessors) { yield break; }
                foreach (CfgNode candidate in this.Predecessors) { yield return candidate; }
            }
        }

        /// <summary>Get an array of nodes that are successors for this one.</summary>
        public CfgNode[] Successors
        {
            get { return (null == _successors) ? null : _successors.ToArray(); }
        }

        IEnumerable<IGraphNode> IGraphNode.Successors
        {
            get
            {
                if (null == this.Successors) { yield break; }
                foreach (CfgNode candidate in this.Successors) { yield return candidate; }
            }
        }
        #endregion

        #region METHODS
        private void AddLink(CfgNode linked, ref List<CfgNode> collection)
        {
            if (null == collection) { collection = new List<CfgNode>(); }
            if (collection.Contains(linked)) { return; }
            collection.Add(linked);
            // TODO : Prevent loops.
            return;
        }

        internal void DumpGraph()
        {
            foreach (CfgNode scannedNode in EnumerateNodes()) {
                StringBuilder builder = new StringBuilder();
                CfgNode[] nodes = scannedNode.Predecessors;
                
                if ((null != nodes) && (0 < nodes.Length)) {
                    foreach (CfgNode scannedPredecessor in nodes) {
                        if (0 < builder.Length) { builder.Append(", "); }
                        builder.Append(scannedPredecessor.NodeId.ToString());
                    }
                }
                if (0 < builder.Length) { builder.Append(" -> "); }
                builder.AppendFormat("[[{0} {1}]]", scannedNode.NodeId, scannedNode.AdditionalInfo);
                bool firstSuccessor = true;
                nodes = scannedNode.Successors;
                if ((null != nodes) && (0 < nodes.Length)) {
                    foreach (CfgNode scannedSuccessor in nodes) {
                        builder.Append(firstSuccessor ? " -> " : ", ");
                        firstSuccessor = false;
                        builder.Append(scannedSuccessor.NodeId.ToString());
                    }
                }
                Console.WriteLine(builder.ToString());
            }
            return;
        }

        /// <summary>Enumerate each node from the graph in no particular order.</summary>
        /// <returns></returns>
        internal IEnumerable<CfgNode> EnumerateNodes()
        {
            List<CfgNode> alreadyScanned = new List<CfgNode>();
            List<CfgNode> pendingNodes = new List<CfgNode>();
            CfgNode entryNode = FindEntryNode();

            pendingNodes.Add(entryNode);
            while (0 < pendingNodes.Count) {
                CfgNode result = pendingNodes[0];

                pendingNodes.RemoveAt(0);
                alreadyScanned.Add(result);
                yield return result;
                CfgNode[] successors = result.Successors;
                if ((null == successors) || (0 == successors.Length)) { continue; }
                foreach (CfgNode scannedNode in result.Successors) {
                    if (alreadyScanned.Contains(scannedNode)) { continue; }
                    if (!pendingNodes.Contains(scannedNode)) {
                        pendingNodes.Add(scannedNode);
                    }
                }
            }
            yield break;
        }

        /// <summary>Find the <see cref="BlockNode"/> within the graph that cover the given
        /// method relative byte range.</summary>
        /// <param name="methodOffset"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        internal BlockNode FindHostingNode(uint methodOffset, uint size)
        {
#if DBGCFG
            if (DebugEnabled) { Console.WriteLine("Seeking block hosting @{0:X4}", methodOffset); }
#endif
            foreach (CfgNode candidate in EnumerateNodes()) {
                BlockNode blockNode = candidate as BlockNode;

                if (null == blockNode) { continue; }
#if DBGCFG
                if (DebugEnabled) { Console.WriteLine("Considering block #{0}", blockNode.NodeId); }
#endif
                if (blockNode.IsCovering(methodOffset, size)) {
#if DBGCFG
                    if (DebugEnabled) {
                        Console.WriteLine("Block #{0} found to host @{1:X4}",
                            blockNode.NodeId, methodOffset);
                    }
#endif
                    return blockNode;
                }
            }
#if DBGCFG
            if (DebugEnabled) { Console.WriteLine("WARNING : No block found hosting @{0:X4}", methodOffset); }
#endif
            return null;
        }

        /// <summary>Starting from a node in a graph, find the entry node for this graph.</summary>
        /// <returns>The entry node for the graph the current instance belongs to.</returns>
        internal CfgNode FindEntryNode()
        {
            CfgNode result = this;
            List<CfgNode> alreadyScanned = new List<CfgNode>();
            List<CfgNode> pendingNodes = new List<CfgNode>();
            while (!result.IsEntryNode) {
                foreach (CfgNode scannedNode in result.Predecessors) {
                    if (alreadyScanned.Contains(scannedNode)) { continue; }
                    if (!pendingNodes.Contains(scannedNode)) { pendingNodes.Add(scannedNode); }
                }
                result = pendingNodes[0];
                pendingNodes.RemoveAt(0);
            }
            return result;
        }

        /// <summary>Retrieve a matrix of nodes adjacency in the graph this node belongs
        /// to.
        /// TODO : Optimize processing time.</summary>
        /// <returns>A matrix where the rows (first index) are thr predecessors and the
        /// columns (second index) are the successors. Whenever X -> Y then result[X][Y]
        /// is true.</returns>
        internal bool[][] GetAdjacencyMatrix()
        {
            // Make sure to operate on the entry node.
            if (!IsEntryNode) { return FindEntryNode().GetAdjacencyMatrix(); }
            List<CfgNode> allNodes = new List<CfgNode>();
            foreach (CfgNode node in EnumerateNodes()) { allNodes.Add(node); }
            allNodes.Sort(delegate(CfgNode x, CfgNode y)
            {
                if (x.NodeId == y.NodeId) { return 0; }
                return (x.NodeId < y.NodeId) ? -1 : 1;
            });
            int matrixSize = allNodes.Count;
            bool[][] result = new bool[matrixSize][];
            for (int index = 0; index < matrixSize; index++) {
                result[index] = new bool[matrixSize];
            }

            for (int index = 0; index < matrixSize; index++) {
                CfgNode scannedNode = allNodes[index];
                CfgNode[] successors = scannedNode.Successors;
                if (null == successors) { continue; }
                bool[] adjacencyRow = result[index];
                foreach (CfgNode successor in successors) {
                    int candidateIndex = allNodes.FindIndex(delegate(CfgNode candidate) {
                        return object.ReferenceEquals(successor, candidate);
                    });
                    adjacencyRow[candidateIndex] = true;
                }
            }
            return result;
        }

        internal static void Link(CfgNode predecessor, CfgNode successor)
        {
            if (object.ReferenceEquals(predecessor, successor)) {
                throw new InvalidOperationException();
            }
#if DBGCFG
            if (   (null != predecessor)
                && (null != successor)
                && (predecessor.DebugEnabled || successor.DebugEnabled))
            {
                Console.WriteLine("[{0}] -> [{1}]", predecessor.NodeId, successor.NodeId);
            }
#endif
            predecessor.AddLink(successor, ref predecessor._successors);
            successor.AddLink(predecessor, ref successor._predecessors);
            return;
        }

        /// <summary>Transfer the successors of this node to the given one. This is for
        /// use during node splits. The receiving node must not have any successor yet.</summary>
        /// <param name="to">The receiving node.</param>
        protected void TransferSuccessors(CfgNode to)
        {
            if (null != to._successors) { throw new InvalidOperationException(); }
            if (null == this._successors) { return; }
            to._successors = this._successors;
            this._successors = null;
            // Must also adjust predecessors in successors.
            foreach (CfgNode target in to._successors) {
                List<CfgNode> targetPredecessors = target._predecessors;
                int targetPredecessorsCount = targetPredecessors.Count;
                bool replacementFound = false;
                for (int index = 0; index < targetPredecessorsCount; index++) {
                    if (!object.ReferenceEquals(this, targetPredecessors[index])) {
                        continue;
                    }
                    replacementFound = true;
                    targetPredecessors[index] = to;
                    break;
                }
                if (!replacementFound) { throw new ApplicationException(); }
            }
            return;
        }
        #endregion

        #region FIELDS
        private List<CfgNode> _predecessors;
        private List<CfgNode> _successors;
#if DBGCFG
        private static uint NextNodeId = 1;
#endif
        #endregion
    }
}
