// <copyright file="XapInfo.cs" company="Jim Evans">
//
// Copyright 2014 Jim Evans
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace ConsoleDriverApplication
{
    /// <summary>
    /// Class that gives the information about a Windows Phone application bundle.
    /// </summary>
    public class XapInfo
    {
        private XapInfo(string archiveFilePath)
        {
            this.ArchiveFilePath = archiveFilePath;
        }

        /// <summary>
        /// Gets the application ID.
        /// </summary>
        public Guid? ApplicationId
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the version of the manifest.
        /// </summary>
        public Version ManifestVersion
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether or not the application is native.
        /// </summary>
        public bool IsNative
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the file path to the archive.
        /// </summary>
        public string ArchiveFilePath
        {
            get;
            private set;
        }

        /// <summary>
        /// Reads the application info.
        /// </summary>
        /// <param name="appArchiveFilePath">Path to the file of the application bundle.</param>
        /// <returns>The <see cref="XapInfo"/> containing the information about the application.</returns>
        public static XapInfo ReadApplicationInfo(string appArchiveFilePath)
        {
            XapInfo appInfo = new XapInfo(appArchiveFilePath);
            try
            {
                // Do not use "using" for the FileStream. The ZipArchive will close/dispose the stream unless
                // we specify otherwise.
                FileStream appArchiveFileStream = new FileStream(appArchiveFilePath, FileMode.Open, FileAccess.Read);
                using (ZipArchive zipArchive = new ZipArchive(appArchiveFileStream, ZipArchiveMode.Read))
                {
                    ZipArchiveEntry appManifestEntry = zipArchive.GetEntry("WMAppManifest.xml");
                    using (Stream appManifestFileStream = appManifestEntry.Open())
                    {
                        XPathDocument manifestDocument = new XPathDocument(appManifestFileStream);
                        XPathNavigator manifestNavigator = manifestDocument.CreateNavigator();
                        XPathNavigator appNodeNavigator = manifestNavigator.SelectSingleNode("//App");
                        appInfo.ApplicationId = new Guid?(new Guid(appNodeNavigator.GetAttribute("ProductID", string.Empty)));
                        string attribute = appNodeNavigator.GetAttribute("RuntimeType", string.Empty);
                        if (attribute.Equals("Modern Native", StringComparison.OrdinalIgnoreCase))
                        {
                            appInfo.IsNative = true;
                        }

                        manifestNavigator.MoveToFirstChild();
                        appInfo.ManifestVersion = new Version(manifestNavigator.GetAttribute("AppPlatformVersion", string.Empty));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ConsoleDriverException("Unexpected error reading application information.", ex);
            }

            return appInfo;
        }

        /// <summary>
        /// Extracts the icon file from the application bundle.
        /// </summary>
        /// <returns>The full path to the extracted icon file.</returns>
        public string ExtractIconFile()
        {
            return this.ExtractFileFromXap(@"Assets\ApplicationIcon.png");
        }

        /// <summary>
        /// Deletes a file from the application bundle.
        /// </summary>
        /// <param name="pathInArchive">The relative path of the file inside the application bundle.</param>
        public void DeleteFileFromXap(string pathInArchive)
        {
            // Do not use "using" for the FileStream. The ZipArchive will close/dispose the stream unless
            // we specify otherwise.
            FileStream appArchiveFileStream = new FileStream(this.ArchiveFilePath, FileMode.Open, FileAccess.ReadWrite);
            using (ZipArchive zipArchive = new ZipArchive(appArchiveFileStream, ZipArchiveMode.Update))
            {
                ZipArchiveEntry iconFileEntry = zipArchive.GetEntry(pathInArchive);
                iconFileEntry.Delete();
            }
        }

        /// <summary>
        /// Inserts a file into the application bundle.
        /// </summary>
        /// <param name="filePath">The file to insert into the application bundle.</param>
        /// <param name="pathInArchive">The relative path of the file inside the application bundle.</param>
        public void InsertFileIntoXap(string filePath, string pathInArchive)
        {
            // Do not use "using" for the FileStream. The ZipArchive will close/dispose the stream unless
            // we specify otherwise.
            FileStream appArchiveFileStream = new FileStream(this.ArchiveFilePath, FileMode.Open, FileAccess.ReadWrite);
            using (ZipArchive zipArchive = new ZipArchive(appArchiveFileStream, ZipArchiveMode.Update))
            {
                ZipArchiveEntry iconFileEntry = zipArchive.CreateEntry(pathInArchive, CompressionLevel.Fastest);
                using (Stream iconFileStream = iconFileEntry.Open())
                {
                    if (iconFileStream == null)
                    {
                        throw new ConsoleDriverException("Could not get file stream for icon from application archive.");
                    }

                    using (FileStream inputFileStream = new FileStream(filePath, FileMode.Open))
                    {
                        inputFileStream.CopyTo(iconFileStream);
                    }
                }
            }
        }

        /// <summary>
        /// Extracts a file from the application bundle.
        /// </summary>
        /// <param name="pathInArchive">The relative path of the file inside the application bundle.</param>
        /// <returns>The full path to the extracted file.</returns>
        public string ExtractFileFromXap(string pathInArchive)
        {
            string result = string.Empty;

            // Do not use "using" for the FileStream. The ZipArchive will close/dispose the stream unless
            // we specify otherwise.
            FileStream appArchiveFileStream = new FileStream(this.ArchiveFilePath, FileMode.Open, FileAccess.Read);
            using (ZipArchive zipArchive = new ZipArchive(appArchiveFileStream, ZipArchiveMode.Read))
            {
                string tempFileName = Path.GetTempFileName();
                ZipArchiveEntry iconFileEntry = zipArchive.GetEntry(pathInArchive);
                using (Stream iconFileStream = iconFileEntry.Open())
                {
                    if (iconFileStream == null)
                    {
                        throw new ConsoleDriverException("Could not get file stream for icon from application archive.");
                    }

                    using (FileStream iconOutputFileStream = new FileStream(tempFileName, FileMode.Create))
                    {
                        iconFileStream.CopyTo(iconOutputFileStream);
                    }
                }

                result = tempFileName;
            }

            return result;
        }
    }
}
