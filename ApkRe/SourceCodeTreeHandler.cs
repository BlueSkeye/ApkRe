using System;
using System.IO;

using com.rackham.ApkJava.API;

namespace com.rackham.ApkRe
{
    internal class SourceCodeTreeHandler
    {
        #region CONSTRUCTORS
        internal SourceCodeTreeHandler(DirectoryInfo baseSourceCodeDirectory)
        {
            if (null == baseSourceCodeDirectory) { throw new ArgumentNullException(); }
            if (!baseSourceCodeDirectory.Exists) { throw new ArgumentException(); }
            _baseDirectory = baseSourceCodeDirectory;
            return;
        }
        #endregion

        #region METHODS
        internal FileInfo GetClassFileName(IAnnotatableClass item, bool doNotCreate = false)
        {
            string[] packageNameItems;
            string simpleClassName = Helpers.GetClassAndPackageName(item, out packageNameItems);
            DirectoryInfo currentDirectory = _baseDirectory;
            if (null != packageNameItems) {
                for (int index = 0; index < packageNameItems.Length; index++) {
                    DirectoryInfo nextDirectory =
                        new DirectoryInfo(Path.Combine(currentDirectory.FullName, packageNameItems[index]));
                    if (!nextDirectory.Exists && !doNotCreate) {
                        nextDirectory.Create();
                        nextDirectory.Refresh();
                    }
                    currentDirectory = nextDirectory;
                }
            }
            // Looks like the FileInfo instance Create call will from time to time trigger an
            // exception when we later attempt to open the file. So we stopped creating the file
            // here.
            return new FileInfo(Path.Combine(currentDirectory.FullName, simpleClassName + ".java"));
        }
        #endregion

        #region FIELDS
        private DirectoryInfo _baseDirectory;
        #endregion
    }
}
