using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkRe.CFG
{
    public class DirectedGraphCycleFinder
    {
        #region CONSTRUCTORS
        /// <summary>Create an instance of the cycle finder for the graph the
        /// given node belongs to.</summary>
        /// <param name="node">A node from the graph.</param>
        public DirectedGraphCycleFinder(IGraphNode node)
        {
            if (null == node) { throw new ArgumentNullException(); }
            _node = node;
            return;
        }
        #endregion

        #region METHODS
        /// <summary>Retrieves the index for the given node.</summary>
        /// <param name="candidate">The candidate node.</param>
        /// <returns>An index that is used for referencing the candidate node in various
        /// arrays.</returns>
        private int GetParticipatingNodeIndex(IGraphNode candidate)
        {
            int participatingNodesCount = _participatingNodes.Length;
            for (int result = 0; result < participatingNodesCount; result++) {
                if (object.ReferenceEquals(_participatingNodes[result], candidate)) {
                    return result;
                }
            }
            throw new ArgumentException();
        }

        private void InitializeResolution()
        {
            // Build an adjacency list for the graph.
            List<IGraphNode> alreadyWalked = new List<IGraphNode>();
            Dictionary<IGraphNode, List<IGraphNode>> connections =
                new Dictionary<IGraphNode, List<IGraphNode>>();
            Stack<IGraphNode> pendingNodes = new Stack<IGraphNode>();
            pendingNodes.Push(_node);
            while (0 < pendingNodes.Count) {
                IGraphNode scannedNode = pendingNodes.Pop();
                if (alreadyWalked.Contains(scannedNode)) { continue; }
                alreadyWalked.Add(scannedNode);
                List<IGraphNode> successors = new List<IGraphNode>();
                if (null != scannedNode.Successors) {
                    foreach (IGraphNode successor in scannedNode.Successors) {
                        successors.Add(successor);
                        pendingNodes.Push(successor);
                    }
                }
                connections[scannedNode] = successors;
                if (null != scannedNode.Predecessors) {
                    foreach (IGraphNode predecessor in scannedNode.Predecessors) {
                        pendingNodes.Push(predecessor);
                    }
                }
            }
            Dictionary<IGraphNode, List<IGraphNode>>.KeyCollection allNodes = connections.Keys;
            int nodesCount = allNodes.Count;
            _originalAdjacencyList = new int[nodesCount][];
            _participatingNodes = new IGraphNode[nodesCount];
            int nodeIndex = 0;
            foreach (IGraphNode scannedNode in allNodes) {
                _participatingNodes[nodeIndex++] = scannedNode;
            }
            foreach (IGraphNode scannedNode in allNodes) {
                List<IGraphNode> adjacents = connections[scannedNode];
                int adjacentsCount = adjacents.Count;
                int[] scannedNodeAdjacents = new int[adjacentsCount];
                _originalAdjacencyList[GetParticipatingNodeIndex(scannedNode)] =
                    scannedNodeAdjacents;
                for (int index = 0; index < adjacentsCount; index++) {
                    scannedNodeAdjacents[index] =
                        GetParticipatingNodeIndex(adjacents[index]);
                }
            }
            return;
        }

        /// <summary>Will find all circuits in the graph this cycle finder is bound to.
        /// </summary>
        /// <returns>A list of circuits that have been found. The list may be empty.
        /// </returns>
        public List<CircuitDefinition> Resolve()
        {
            InitializeResolution();
            HawickJamesCycleFinder resolver =
                new HawickJamesCycleFinder(_originalAdjacencyList, _participatingNodes);
            
            return resolver.Resolve();
        }
        #endregion

        #region FIELDS
        /// <summary>The node that has been provided at construction time.</summary>
        private IGraphNode _node;
        /// <summary>An initial adjacency list where each value from the first index
        /// refers to the node having the same index in <see cref="_participatingNodes"/>
        /// Values from the second index also refers to participating nodes.</summary>
        private int[][] _originalAdjacencyList;
        /// <summary>An array of all the nodes participating in the graph at cycle time
        /// detction.</summary>
        private IGraphNode[] _participatingNodes;
        #endregion

        #region INNER CLASSES
        /// <summary>An implementation of the Hawick & James algorithm.</summary>
        private class HawickJamesCycleFinder
        {
            #region CONSTRUCTORS
            /// <summary>Create an instance of the finder using an adjacency list and an
            /// array of graph nodes. First index of each array match those of the other
            /// array. That is, adjacenccyList[0] is for participatingNodes[0] node.</summary>
            /// <param name="originalAdjacencyList">An adjacency list. Each entry in the
            /// array is in turn an array of successours for the considered node.</param>
            /// <param name="participatingNodes">An array of graph nodes.</param>
            internal HawickJamesCycleFinder(int[][] originalAdjacencyList,
                IGraphNode[] participatingNodes)
            {
                _participatingNodes = participatingNodes;
                _nodesCount = participatingNodes.Length;
                _successors = new List<int>[_nodesCount];
                _blockers = new List<int>[_nodesCount];
                _blockedNodes = new bool[_nodesCount];
                for (int index = 0; index < _nodesCount; index++) {
                    _successors[index] = new List<int>(originalAdjacencyList[index]);
                    _blockers[index] = new List<int>();
                }
                _circuitCandidate = new Stack<int>();
                return;
            }
            #endregion

            #region METHODS
            private List<CircuitDefinition> IsInCircuit(int candidate)
            {
                List<CircuitDefinition> result = null;
                _circuitCandidate.Push(candidate);
                _blockedNodes[candidate] = true;
                List<int> candidateSuccessors = _successors[candidate];
                foreach(int nextWalkedNodeIndex in candidateSuccessors) {
                    if (nextWalkedNodeIndex < _searchedNodeIndex) { continue; }
                    if (nextWalkedNodeIndex == _searchedNodeIndex) {
                        // we have a circuit,
                        if (_circuitCandidate.Count > _nodesCount) { throw new ApplicationException(); }
                        CircuitDefinition newCircuit =
                            new CircuitDefinition(_searchedNodeIndex, _circuitCandidate, _participatingNodes);
                        if (null == result) { result = new List<CircuitDefinition>(); }
                        result.Add(newCircuit);
                        continue;
                    }
                    if (_blockedNodes[nextWalkedNodeIndex]) { continue; }
                    // Iterate
                    List<CircuitDefinition> innerCircuits = IsInCircuit(nextWalkedNodeIndex);
                    if (null != innerCircuits) {
                        if (null == result) { result = new List<CircuitDefinition>(); }
                        result.AddRange(innerCircuits);
                    }
                }
                if (null != result) { Unblock(candidate); }
                else {
                    foreach(int scannedSuccessor in candidateSuccessors) {
                        if (scannedSuccessor < _searchedNodeIndex) { continue; }
                        if (!_blockers[scannedSuccessor].Contains(candidate)) {
                            _blockers[scannedSuccessor].Add(candidate);
                        }
                    }
                }
                _circuitCandidate.Pop();
                return result;
            }

            /// <summary>Main resolution method. Will consider each node from the graph in turn
            /// as a potential initial node in a circuit.</summary>
            /// <returns>The resolution result that is a list (maybe empty) of cycles that would
            /// have been found in the graph.</returns>
            internal List<CircuitDefinition> Resolve()
            {
                List<CircuitDefinition> result = new List<CircuitDefinition>();
                for (_searchedNodeIndex = 0; _searchedNodeIndex < _nodesCount; _searchedNodeIndex++) {
                    for (int nodeIndex = 0; nodeIndex < _nodesCount; nodeIndex++) {
                        _blockedNodes[nodeIndex] = false;
                        _blockers[nodeIndex].Clear();
                    }
                    List<CircuitDefinition> circuits = IsInCircuit(_searchedNodeIndex);
                    if (null != circuits) { result.AddRange(circuits); }
                }
                return result;
            }

            private void Unblock(int candidate)
            {
                List<int> pendings = new List<int>();
                pendings.Add(candidate);

                while (0 < pendings.Count) {
                    int scannedNodeIndex = pendings[0];
                    pendings.RemoveAt(0);
                    _blockedNodes[scannedNodeIndex] = false;
                    List<int> scannedList = _blockers[scannedNodeIndex];
                    for (int index = 0; index < scannedList.Count; index++) {
                        int successor = scannedList[index];
                        if (scannedList.Remove(successor)) { index--; }
                        if (_blockedNodes[successor]) { pendings.Add(successor); }
                    }
                }
                return;
            }
            #endregion

            #region FIELDS
            /// <summary>Array index match those of <see cref="_participatingNodes"/></summary>
            private bool[] _blockedNodes;
            /// <summary></summary>
            private List<int>[] _blockers;
            /// <summary>The currently considered candidate circuit.</summary>
            private Stack<int> _circuitCandidate = null;
            private int _nodesCount = 0;
            private IGraphNode[] _participatingNodes;
            /// <summary>Index in <see cref="_participatingNodes"/> of the node that is
            /// currently considered as the potential first one in a circuit.</summary>
            private int _searchedNodeIndex;
            /// <summary>An array of lists. Each index in the array match those of the
            /// <see cref="_participatingNodes"/>. The associated list (that may be empty)
            /// contains the indices of the successor nodes. Those indices also match with
            /// <see cref="_participatingNodes"/></summary>
            private List<int>[] _successors;
            #endregion
        }

        /// <summary>A very limited test node class for testing purpose only.</summary>
        public class TestNode : IGraphNode
        {
            #region CONSTRUCTORS
            public TestNode(string label)
            {
                Label = label;
                return;
            }
            #endregion

            #region PROPERTIES
            public bool IsEntryNode
            {
                get { return (null == _predecessors) || (0 == _predecessors.Count); }
            }

            public bool IsExitNode
            {
                get { return (null == _successors) || (0 == _successors.Count); }
            }

            internal string Label { get; private set; }

            public IEnumerable<IGraphNode> Predecessors
            {
                get { return _predecessors; }
            }

            public IEnumerable<IGraphNode> Successors
            {
                get { return _successors; }
            }
            #endregion

            #region METHODS
            public void AddSuccessor(TestNode other)
            {
                if (null == other) { throw new ArgumentNullException(); }
                if (null == _successors) { _successors =new List<IGraphNode>(); }
                if (!_successors.Contains(other)) { _successors.Add(other); }
                if (null == other._predecessors) { other._predecessors = new List<IGraphNode>(); }
                if (!other._predecessors.Contains(this)) {
                    other._predecessors.Add(this);
                }
                return;
            }

            public override string ToString()
            {
                return Label ?? "UNKNOWN";
            }
            #endregion

            #region FIELDS
            private List<IGraphNode> _predecessors;
            private List<IGraphNode> _successors;
            #endregion
        }
        #endregion
    }
}
