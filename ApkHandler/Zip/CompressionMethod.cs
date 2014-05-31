using System;

namespace com.rackham.ApkHandler.Zip
{
    internal enum CompressionMethod
    {
        /// <summary>No compression</summary>
        Stored = 0,
        Shrunk,
        ReducedCompressionFactor1,
        ReducedCompressionFactor2,
        ReducedCompressionFactor3,
        ReducedCompressionFactor4,
        Imploded,
        ReservedTokenizing,
        Deflated,
        Deflated64,
        OldIbmTerse,
        PkwareReserved1,
        Bzip2,
        PkwareReserved2,
        Lzma,
        PkwareReserved3,
        PkwareReserved4,
        PkwareReserved5,
        IbmTerse,
        Lz77,
        WavPack = 97,
        PPMd,
    }
}