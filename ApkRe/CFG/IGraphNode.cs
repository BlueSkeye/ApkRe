using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkRe.CFG
{
    public interface IGraphNode
    {
        /// <summary>Returns true if the node has no predecessor.</summary>
        bool IsEntryNode { get; }

        /// <summary>Returns true if the node has no successor.</summary>
        bool IsExitNode { get; }

        /// <summary>Provides an object that will enumerate every predecessors
        /// of this node.</summary>
        IEnumerable<IGraphNode> Predecessors { get; }

        /// <summary>Provide and object that will enumerate every successors
        /// of this node.</summary>
        IEnumerable<IGraphNode> Successors { get; }
    }
}
