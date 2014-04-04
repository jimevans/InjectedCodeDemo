using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleDriverApplication
{
    class ZipArchiveFile
    {
        private long dataPosition;
        private ushort compressMethod;

        private ZipArchiveFile()
        {
        }

        internal string Name
        {
            get;
            private set;
        }
        
        internal static ZipArchiveFile ReadHeader(FileStream stream)
        {
            BinaryReader binaryReader = new BinaryReader(stream);
            uint num = binaryReader.ReadUInt32();
            if (num != 67324752u)
            {
                return null;
            }

            ZipArchiveFile zipArchiveFile = new ZipArchiveFile();
            binaryReader.ReadUInt16();
            binaryReader.ReadUInt16();
            zipArchiveFile.compressMethod = binaryReader.ReadUInt16();
            binaryReader.ReadUInt16();
            binaryReader.ReadUInt16();
            binaryReader.ReadUInt32();
            uint num2 = binaryReader.ReadUInt32();
            binaryReader.ReadUInt32();
            ushort num3 = binaryReader.ReadUInt16();
            ushort count = binaryReader.ReadUInt16();
            byte[] array = new byte[(int)num3];
            binaryReader.Read(array, 0, (int)num3);
            zipArchiveFile.Name = Encoding.UTF8.GetString(array);
            binaryReader.ReadBytes((int)count);
            zipArchiveFile.dataPosition = binaryReader.BaseStream.Position;
            binaryReader.BaseStream.Seek((long)((ulong)num2), SeekOrigin.Current);
            return zipArchiveFile;
        }

        internal Stream GetUncompressedStream(Stream zipStream)
        {
            zipStream.Seek(this.dataPosition, SeekOrigin.Begin);
            ushort num = this.compressMethod;
            if (num == 0)
            {
                return zipStream;
            }

            if (num != 8)
            {
                return null;
            }

            return new DeflateStream(zipStream, CompressionMode.Decompress);
        }
    }
}
