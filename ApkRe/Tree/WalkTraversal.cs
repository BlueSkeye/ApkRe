using System;

namespace com.rackham.ApkRe.Tree
{
    /// <summary>Values for a parameter of a <see cref="WalkNodeHandlerDelegate"/>
    /// Denotes how we arrived on the node that is provided to the delegate.</summary>
    internal enum WalkTraversal
    {
        /// <summary>This value is used for the one and unique time a node is
        /// walked when mode is <see cref="WalkMode.SonsThenFather"/> or
        /// <see cref="WalkNode.FatherThenSons"/></summary>
        CurrentNode,
        /// <summary>This value is used the first time a node is walked when
        /// mode is <see cref="WalkMode.TransitBeforeAndAfter"/> or is
        /// <see cref="WalkMode.FullTransit"/></summary>
        BeforeTransit,
        /// <summary>This value is used the last time a node is walked when
        /// mode is <see cref="WalkMode.TransitBeforeAndAfter"/> or is
        /// <see cref="WalkMode.FullTransit"/></summary>
        AfterTransit,
        /// <summary>This value is used when mode is <see cref="WalkMode.FullTransit"/>
        /// and the node has already been enumerated and will later be again.
        /// </summary>
        Transit,
    }
}
