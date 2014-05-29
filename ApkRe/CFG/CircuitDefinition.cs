using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkRe.CFG
{
    /// <summary>The resolution algorithm creates an instance of this class each
    /// time it detects a new circuit.</summary>
    public class CircuitDefinition
    {
        #region CONSTRUCTORS
        internal CircuitDefinition(int start, Stack<int> circuit,
            IGraphNode[] participatingNodes)
        {
            int[] stackedContent = new int[circuit.Count];
            circuit.CopyTo(stackedContent, 0);
            _circuit = new IGraphNode[stackedContent.Length];
            for (int index = stackedContent.Length - 1, circuitIndex = 0;
                0 <= index;
                index--, circuitIndex++)
            {
                _circuit[circuitIndex] = participatingNodes[stackedContent[index]];
            }
            Start = start;
            return;
        }
        #endregion

        #region PROPERTIES
        public int Start { get; private set; }
        #endregion

        #region METHODS
        /// <summary>Will print the circuit to the console in nodes order, using the
        /// <see cref="ToString"/> method on each node in turn and displaying whatever
        /// this method returns.</summary>
        public void Print()
        {
            foreach(IGraphNode node in _circuit) {
                Console.Write("{0} ", node.ToString());
            }
            Console.WriteLine();
            return;
        }
        #endregion

        #region METHODS
        /// <summary>Reorder circuit definition so as to have the given node
        /// being the first.</summary>
        /// <param name="node">The node that must now be the first one.</param>
        public void SetStartNode(IGraphNode node)
        {
            if (null == node) { throw new ArgumentNullException(); }
            int shiftBy;
            for (shiftBy = 0; shiftBy < _circuit.Length; shiftBy++) {
                if (object.ReferenceEquals(node, _circuit[shiftBy])) { break; }
            }
            if (shiftBy >= _circuit.Length) { throw new ArgumentException(); }
            if (0 == shiftBy) { return; }
            IGraphNode[] newCircuit = new IGraphNode[_circuit.Length];
            int firstMoveSize = _circuit.Length - shiftBy;
            Buffer.BlockCopy(_circuit, shiftBy, newCircuit, 0, firstMoveSize);
            Buffer.BlockCopy(_circuit, 0, newCircuit, firstMoveSize, shiftBy);
            _circuit = newCircuit;
            return;
        }
        #endregion

        #region FIELDS
        private IGraphNode[] _circuit;
        #endregion
    }
}
