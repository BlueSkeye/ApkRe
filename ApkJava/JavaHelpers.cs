using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.rackham.ApkJava
{
    public static class JavaHelpers
    {
        public static string GetUndecoratedClassName(string candidate)
        {
            return candidate;

            //// TODO : Handle inner classes naming convention.
            //if (('L' != candidate[0]) || (';' != candidate[candidate.Length - 1])) {
            //    return candidate;
            //}
            //return candidate.Substring(1, candidate.Length - 2);
        }
    }
}
