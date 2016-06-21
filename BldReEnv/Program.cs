using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;

namespace BldReEnv
{
    public static class Program
    {
        #region PROPERTIES
        private static string ExecutableName
        {
            get { return new FileInfo(Assembly.GetEntryAssembly().Location).Name; }
        }

        private static string ProgramNameAndVersion
        {
            get
            {
                AssemblyName assemblyName = Assembly.GetEntryAssembly().GetName();
                return assemblyName.Name + " " + assemblyName.Version.ToString();
            }
        }
        #endregion

        #region METHODS
        private static bool AreHashEqual(FileInfo existing, ZipArchiveEntry duplicate)
        {
            byte[] originalHash;
            byte[] otherHash;
            try {
                using(FileStream original = File.Open(existing.FullName, FileMode.Open, FileAccess.Read)) {
                    if (null == (originalHash = HashFile(original))) { return false; }
                }
                using(Stream other = duplicate.Open()) {
                    if (null == (otherHash = HashFile(other))) { return false; }
                }
                if (originalHash.Length == otherHash.Length) {
                    for(int index = 0; index < originalHash.Length; index++) {
                        if (originalHash[index] != otherHash[index]) {
                            WriteError("Hashes don't match.");
                            return false;
                        }
                    }
                    return true;
                }
                return false;
            }
            catch (Exception e) {
                WriteError("Error while trying to compare hash. Error {0}",
                    e.Message);
                return false;
            }
        }

        private static bool CleanUpOutputDirectory()
        {
            bool result = true;
            foreach(FileInfo deletedFile in RecursivelyEnumerateFiles(_outputDirectory, ".class")) {
                try { deletedFile.Delete(); }
                catch (Exception e) {
                    WriteError("Failed to delete '{0} file. Error : {1}",
                        deletedFile.FullName, e.Message);
                    result = false;
                }
            }
            return result;
        }

        private static void DisplayUsage(bool skipFirstLine)
        {
            if (skipFirstLine) { WriteMessage(""); }
            WriteMessage(ExecutableName + " -h | -e [-f] <input dir> <output dir>");
            WriteMessage("");
            WriteMessage("-e : extract content from every .jar files in <input dir>");
            WriteMessage("     and copy .class files into <output dir>. Output dir");
            WriteMessage("     must not exist or be empty unless the -f flag is set");
            WriteMessage("     in which case already existing .class files in output");
            WriteMessage("     dir will be deleted prior to extraction.");
            WriteMessage("-h : Display this notice.");
            return;
        }

        private static bool Extract(FileInfo from)
        {
            if (null == from) { throw new ArgumentNullException(); }
            if (!from.Exists) { throw new ArgumentException(); }
            try {
                bool result = true;
                byte[] buffer = new byte[65536];

                WriteMessage("Extracting '{0}'", from.Name);
                using (ZipArchive input = ZipFile.OpenRead(from.FullName)) {
                    foreach(ZipArchiveEntry scannedEntry in input.Entries) {
                        if (!scannedEntry.FullName.ToLower().EndsWith(".class")) {
                            continue;
                        }
                        FileInfo targetFile = new FileInfo(
                            Path.Combine(_outputDirectory.FullName, scannedEntry.FullName));
                        if (targetFile.Exists) {
                            WriteWarning("Duplicate file '{0}'", scannedEntry.FullName);
                            if (!AreHashEqual(targetFile, scannedEntry)) {
                                WriteError("Files don't match.");
                                result = false;
                            }
                            continue;
                        }
                        if (!targetFile.Directory.Exists) {
                            try { targetFile.Directory.Create(); }
                            catch (Exception e) {
                                WriteError("Failed to create directory for '{0}'. Error : {1}.",
                                    scannedEntry.FullName, e.Message);
                                result = false;
                                continue;
                            }
                        }
                        Stream entryStream = null;
                        try {
                            try { entryStream = scannedEntry.Open(); }
                            catch (Exception e) {
                                WriteError("Failed to acquire compressed content from '{0}'. Error : {1}.",
                                    scannedEntry.FullName, e.Message);
                                result = false;
                                continue;
                            }
                            using (FileStream output = File.Open(targetFile.FullName, FileMode.CreateNew, FileAccess.Write)) {
                                while (true) {
                                    int readCount = entryStream.Read(buffer, 0, buffer.Length);
                                    if (0 == readCount) { break; }
                                    output.Write(buffer, 0, readCount);
                                }
                            }
                        }
                        catch (Exception e) {
                            WriteError("Failed to write file '{0}'. Error : {1}.",
                                scannedEntry.FullName, e.Message);
                            result = false;
                            continue;
                        }
                        finally {
                            if (null != entryStream) { entryStream.Dispose(); }
                        }
                    }
                }
                return result;
            }
            catch (Exception e) {
                WriteError("Failed to extract '{0}'. Error: {1}.",
                    from.FullName, e.Message);
                return false;
            }
        }

        private static byte[] HashFile(Stream content)
        {
            try {
                HashAlgorithm hasher = SHA1.Create();
                byte[] buffer = new byte[8192];
                using (MemoryStream trashStream = new MemoryStream()) {
                    using (CryptoStream hashingStream = new CryptoStream(trashStream, hasher, CryptoStreamMode.Write)) {
                        while (true) {
                            int readCount = content.Read(buffer, 0, buffer.Length);
                            if (0 == readCount) { break; }
                            hashingStream.Write(buffer, 0, readCount);
                        }
                    }
                    return hasher.Hash;
                }
            }
            catch (Exception e) {
                WriteError("Failed to hash a file. Error {0}.", e.Message);
                return null;
            }
        }

        public static int Main(string[] args)
        {
            Console.WriteLine(ProgramNameAndVersion);
            if (!ParseArgs(args)) {
                Console.WriteLine();
                DisplayUsage(true);
                return (int)ResultCode.InvalidArguments;
            }
            if (_forceUpdate) {
                WriteMessage("Cleaning output directory.");
                if (!CleanUpOutputDirectory()) {
                    WriteError("Output directory cleanup failed.");
                    return (int)ResultCode.CleanupFailed;
                }
            }
            bool success = true;
            foreach(FileInfo extractedFile in RecursivelyEnumerateFiles(_inputDirectory, ".jar")) {
                success &= Extract(extractedFile);
            }
            return (success) ? (int)ResultCode.Ok : (int)ResultCode.ExtractionFailure;
        }

        private static bool ParseArgs(string[] args)
        {
            if (0 == args.Length) { return false; }
            switch (args[0].ToLower()) {
                case "-h":
                    _displayUsage = true;
                    return true;
                case "-e":
                    break;
                default:
                    WriteError("Unrecognized option '{0}'.", args[0]);
                    return false;
            }
            if (3 > args.Length) {
                WriteError("Incorrect arguments count.");
                return false;
            }
            int inputDirectoryArgIndex;
            if ("-f" == args[1].ToLower()) {
                _forceUpdate = true;
                if (4 != args.Length) {
                    WriteError("Incorrect arguments count.");
                    return false;
                }
                inputDirectoryArgIndex = 2;
            }
            else {
                if (3 != args.Length) {
                    WriteError("Incorrect arguments count.");
                    return false;
                }
                inputDirectoryArgIndex = 1;
            }
            try { _inputDirectory = new DirectoryInfo(args[inputDirectoryArgIndex]); }
            catch (Exception e) {
                WriteError("Input directory argument is not a directory path. Error : {0}",
                    e.Message);
                return false;
            }
            try { _outputDirectory = new DirectoryInfo(args[inputDirectoryArgIndex + 1]); }
            catch (Exception e) {
                WriteError("Output directory argument is not a directory path. Error : {0}",
                    e.Message);
                return false;
            }
            if (!_inputDirectory.Exists) {
                WriteError("The input directory doesn't exist.");
                return false;
            }
            try { _inputDirectory.GetFiles(); }
            catch (Exception e) {
                WriteError("Can't access input directory. Error : {0}.", e.Message);
                return false;
            }
            if (_outputDirectory.Exists && !_forceUpdate) {
                FileInfo[] existingFiles;
                DirectoryInfo[] existingDirectories;
                try {
                    existingFiles = _outputDirectory.GetFiles();
                    existingDirectories = _outputDirectory.GetDirectories();
                }
                catch (Exception e) {
                    WriteError("Can't access output directory. Error : {0}.", e.Message);
                    return false;
                }
                if ((0 != existingFiles.Length) || (0 != existingDirectories.Length)) {
                    WriteError("The output directory is not empty and the -f option is not set.");
                    return false;
                }
            }
            return true;
        }

        private static IEnumerable<FileInfo> RecursivelyEnumerateFiles(DirectoryInfo baseDirectory,
            string fileSuffix)
        {
            if (null == baseDirectory) { throw new ArgumentNullException(); }
            if (!baseDirectory.Exists) { throw new ArgumentException(); }
            if (string.IsNullOrEmpty(fileSuffix)) { throw new ArgumentNullException(); }
            if (fileSuffix.Contains("*")) { throw new ArgumentException(); }
            if (!fileSuffix.StartsWith(".")) { fileSuffix = "." + fileSuffix; }
            fileSuffix = "*" + fileSuffix;
            Stack<DirectoryInfo> pendingDirectories = new Stack<DirectoryInfo>();
            pendingDirectories.Push(baseDirectory);
            while (0 < pendingDirectories.Count) {
                DirectoryInfo scannedDirectory = pendingDirectories.Pop();
                foreach(DirectoryInfo subDirectory in scannedDirectory.GetDirectories()) {
                    pendingDirectories.Push(subDirectory);
                }
                foreach(FileInfo scannedFile in scannedDirectory.GetFiles(fileSuffix)) {
                    yield return scannedFile;
                }
            }
            yield break;
        }

        private static void WriteColoredMessage(ConsoleColor messageColor, string format,
            params object[] args)
        {
            ConsoleColor savedColor = Console.ForegroundColor;
            try {
                Console.ForegroundColor = messageColor;
                Console.WriteLine(format, args);
            }
            finally { Console.ForegroundColor = savedColor; }
        }

        private static void WriteError(string format, params object[] args)
        {
            WriteColoredMessage(ConsoleColor.Red, format, args);
        }

        private static void WriteMessage(string format, params object[] args)
        {
            WriteColoredMessage(ConsoleColor.Green, format, args);
        }

        private static void WriteWarning(string format, params object[] args)
        {
            WriteColoredMessage(ConsoleColor.White, format, args);
        }
        #endregion

        #region FIELDS
        private static bool _displayUsage;
        private static bool _forceUpdate = false;
        private static DirectoryInfo _inputDirectory;
        private static DirectoryInfo _outputDirectory;
        #endregion
    }
}
