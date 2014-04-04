using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleDriverApplication
{
    class ZipArchive
    {
        private FileStream stream;
        private List<ZipArchiveFile> fileList = new List<ZipArchiveFile>();

        internal ZipArchive(FileStream stream)
        {
            this.stream = stream;
            for (ZipArchiveFile item = ZipArchiveFile.ReadHeader(stream); item != null; item = ZipArchiveFile.ReadHeader(stream))
            {
                this.fileList.Add(item);
            }
        }

        internal Stream GetFileStream(string filename)
        {
            foreach (ZipArchiveFile current in this.fileList)
            {
                if (string.Compare(current.Name, filename, true) == 0)
                {
                    return current.GetUncompressedStream(this.stream);
                }
            }

            return null;
        }
    }
}
