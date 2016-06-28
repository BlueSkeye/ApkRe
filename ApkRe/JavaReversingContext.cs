using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace com.rackham.ApkRe
{
    public class JavaReversingContext
    {
        #region CONSTRUCTORS
        private JavaReversingContext()
        {
            return;
        }
        #endregion

        #region PROPERTIES
        internal DirectoryInfo AndroidClassesDirectory { get; private set; }
        internal DirectoryInfo BaseSourceCodeDirectory { get; private set; }
        internal DirectoryInfo JreClassesDirectory { get; private set; }
        internal DirectoryInfo PlatformSpecificClassesDirectory { get; private set; }
        private DirectoryInfo[] SearchOrder;
        #endregion

        #region METHODS
        public static JavaReversingContext Create(DirectoryInfo baseSourceCodeDirectory,
            DirectoryInfo jreClassesDirectory, DirectoryInfo androidClassesDirectory, 
            DirectoryInfo platformSpecificClassesDirectory)
        {
            return new JavaReversingContext() {
                AndroidClassesDirectory = androidClassesDirectory,
                BaseSourceCodeDirectory = baseSourceCodeDirectory,
                JreClassesDirectory = jreClassesDirectory,
                PlatformSpecificClassesDirectory = platformSpecificClassesDirectory,
                SearchOrder = new DirectoryInfo[] { jreClassesDirectory,
                    androidClassesDirectory, platformSpecificClassesDirectory }
            };
        }

        private static readonly string[] ObjectParseResult = new string[] { "java", "lang", "object" };

        private static string[] ParseClassName(string candidate)
        {
            if (string.IsNullOrEmpty(candidate)) { throw new ArgumentNullException(); }
            int candidateLength = candidate.Length;
            if (2 >= candidateLength) { throw new ArgumentException(); }
            // Special case
            if ("Object"== candidate) {
                return (string[])ObjectParseResult.Clone();
            }
            if (('L' != candidate[0]) || (';' != candidate[candidateLength - 1])) {
                throw new ArgumentException();
            }
            candidate = candidate.Substring(1, candidateLength - 2);
            string[] result = candidate.Split('/');
            for(int index = 0; index < result.Length; index++) {
                if (string.IsNullOrEmpty(result[index])) {
                    throw new ArgumentException();
                }
            }
            return result;
        }

        internal FileInfo ResolveClassNameToFile(string className)
        {
            string[] items = ParseClassName(className);
            foreach(DirectoryInfo searchIn in SearchOrder) {
                FileInfo result = TryResolve(searchIn, items);
                if (null != result) { return result; }
            }
            return null;
        }

        private static FileInfo TryResolve(DirectoryInfo searchIn, string[] items)
        {
            int lastItemIndex = items.Length - 1;
            for(int index = 0; index <= lastItemIndex; index++) {
                if (lastItemIndex == index) {
                    FileInfo[] candidates = searchIn.GetFiles(items[index] + ".class");
                    if ((null == candidates) || (0 == candidates.Length)) { return null; }
                    if (2 <= candidates.Length) {
                        Console.WriteLine(
                            "Ambiguous file name encountered while trying to resolve '{0}' in directory '{1}'.",
                            items[index], searchIn.FullName);
                        throw new ApplicationException();
                    }
                    return (0 == candidates.Length) ? null : candidates[0];
                }
                else {
                    DirectoryInfo[] candidates = searchIn.GetDirectories(items[index]);
                    if ((null == candidates) || (0 == candidates.Length)) { return null; }
                    if (2 <= candidates.Length) {
                        Console.WriteLine(
                            "Ambiguous directory name encountered while trying to resolve '{0}' in directory '{1}'.",
                            items[index], searchIn.FullName);
                        throw new ApplicationException();
                    }
                    searchIn = candidates[0];
                }
            }
            // Make the compiler happy. Unreachable code.
            return null;
        }
        #endregion
    }
}
