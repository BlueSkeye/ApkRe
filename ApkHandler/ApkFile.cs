using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

using com.rackham.ApkHandler.Dex.CC;
using com.rackham.ApkHandler.Zip;

namespace com.rackham.ApkHandler
{
    public class ApkFile
    {

        #region PROPERTIES
        public bool IsApkSigned
        {
            get { throw new NotImplementedException(); }
        }
        #endregion

        #region METHODS
        /// <summary>Extract file content into the target directory.</summary>
        /// <param name="from">Input file</param>
        /// <param name="to">target directory</param>
        public static ApkFile Extract(FileInfo from, DirectoryInfo to)
        {
            ZipExtractor.Extract(from, to);
            return new ApkFile();
        }

        /// <summary>Find resources files and load them.</summary>
        /// <param name="from"></param>
        private void FindAndLoadPackages(DirectoryInfo from)
        {
            Stack<DirectoryInfo> pendingDirectories = new Stack<DirectoryInfo>();
            List<FileInfo> resourceFiles = new List<FileInfo>();
            pendingDirectories.Push(from);
            while (0 < pendingDirectories.Count) {
                DirectoryInfo scannedDirectory = pendingDirectories.Pop();
                resourceFiles.AddRange(scannedDirectory.GetFiles(ResourceFilePattern));
                foreach(DirectoryInfo subDirectory in scannedDirectory.GetDirectories()) {
                    pendingDirectories.Push(subDirectory);
                }
            }
            // Here we are with a list of resource files.
            _compressedResources = new List<CompressedResource>();
            foreach (FileInfo resourceFile in resourceFiles) {
                _compressedResources.Add(CompressedResource.LoadFrom(resourceFile));
            }
            return;
        }

        /// <summary>Retrieve the package having the given identifier. This method MUST NOT
        /// be invoked before <see cref="HandleCompressedContents"/> has been invoked.
        /// </summary>
        /// <param name="id">Identifier of the seeked package.</param>
        /// <returns>The retrieved package. If no such package exists an exception is
        /// thrown.</returns>
        internal Package GetPackage(uint id)
        {
            if (null == _compressedResources) { throw new InvalidOperationException(); }
            foreach (CompressedResource candidateResource in _compressedResources) {
                foreach (Package candidatePackage in candidateResource.Packages) {
                    if (candidatePackage.Id == id) { return candidatePackage; }
                }
            }
            throw new ArgumentException("Package not found " + id.ToString());
        }

        /// <summary>Scan a directory and its sub directories for files known to
        /// have their content compressed and restore the original uncompressed
        /// form into another directory. This include the manifest as well as any
        /// compressed resource file.</summary>
        /// <param name="from">The initial directory to be scanned.</param>
        /// <param name="to">The root target directory where uncompressed content
        /// will be produced.</param>
        public void HandleCompressedContents(DirectoryInfo from, DirectoryInfo to)
        {
            // Must load packages first because manifest file may have attributes
            // which value refers to some resource content.
            FindAndLoadPackages(from);
            FileInfo manifestFile = new FileInfo(Path.Combine(from.FullName, ManifestFileName));
            FileInfo outputFile;
            if (manifestFile.Exists) {
                XmlDocument document = null;
                using (FileStream input = File.Open(manifestFile.FullName, FileMode.Open, FileAccess.Read)) {
                    document = CompressedXmlHandler.GetDocument(input, this.GetPackage);
                }
                outputFile = new FileInfo(Helpers.EnsurePath(to, ManifestFileName));
                using (FileStream output = File.Open(outputFile.FullName, FileMode.Create, FileAccess.Write)) {
                    document.Save(output);
                }
            }
            throw new NotImplementedException();
        }

        public void VerifySignature()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region FIELDS
        private const string ManifestFileName = "AndroidManifest.xml";
        private const string ResourceFilePattern = "*.arsc";
        private List<CompressedResource> _compressedResources;
        #endregion
    }
}