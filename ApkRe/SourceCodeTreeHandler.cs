using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace com.rackham.ApkRe
{
    internal class SourceCodeTreeHandler
    {
        #region CONSTRUCTORS
        internal SourceCodeTreeHandler(DirectoryInfo baseDirectory)
        {
            if (null == baseDirectory) { throw new ArgumentNullException(); }
            if (!baseDirectory.Exists) { throw new ArgumentException(); }
            _baseDirectory = baseDirectory;
            return;
        }
        #endregion

        #region METHODS
        internal FileInfo GetClassFileName(string fullClassName, bool doNotCreate = false)
        {
            string[] packageNameItems;
            string simpleClassName = Helpers.GetClassAndPackageName(fullClassName, out packageNameItems);
            DirectoryInfo currentDirectory = _baseDirectory;
            for (int index = 0; index < packageNameItems.Length; index++) {
                DirectoryInfo nextDirectory =
                    new DirectoryInfo(Path.Combine(currentDirectory.FullName, packageNameItems[index]));
                if (!nextDirectory.Exists && !doNotCreate) {
                    nextDirectory.Create();
                    nextDirectory.Refresh();
                }
                currentDirectory = nextDirectory;
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
