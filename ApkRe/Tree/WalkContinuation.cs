using System;

namespace com.rackham.ApkRe.Tree
{
    /// <summary>Possible values returned by <see cref="WalkNodeHandlerDelegate"/></summary>
    internal enum WalkContinuation
    {
        /// <summary>Continue with next item as defined by the walk parameters.</summary>
        Normal,
        /// <summary>Skip other sons of the current node.</summary>
        SkipSons,
        /// <summary>Skip other sons of the current node, as well as brothers of
        /// the current node.</summary>
        SkipBrothers,
        /// <summary>Terminate walk.</summary>
        Terminate,
    }
}
