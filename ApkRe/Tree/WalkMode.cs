using System;

namespace com.rackham.ApkRe.Tree
{
    /// <summary>Kind of walk</summary>
    internal enum WalkMode
    {
        /// <summary>Walk every son before their father.</summary>
        SonsThenFather,
        /// <summary>Walk father then each son in turn. Grandsons are walked
        /// between two adjacent brothers sons.</summary>
        FatherThenSons,
        /// <summary>A father will be walked twice, once before its sons then
        /// once after all sons have been walked.</summary>
        TransitBeforeAndAfter,
        /// <summary>A father will be walked several times. Once before its sons,
        /// then once after each of it sons, including the last one.</summary>
        FullTransit,
    }
}
