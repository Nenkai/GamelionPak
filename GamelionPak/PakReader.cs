using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Buffers.Binary;

using Syroot.BinaryData.Memory;

namespace GamelionPak
{

    public class PakMount
    {
        private CompressedFile _compressedFile;

        public List<PakFile> Files { get; set; } = new();

        // Claw::PakMount::PakMount - 0x1D49D0
        public void Init(string file)
        {
            _compressedFile = new CompressedFile();
            _compressedFile.Open(file);

            // Starting from here, everything is chunked
            byte[] numEntriesBuf = new byte[sizeof(int)];
            _compressedFile.Read(numEntriesBuf, 4);
            int numEntries = BinaryPrimitives.ReadInt32LittleEndian(numEntriesBuf);

            byte[] entries = new byte[numEntries * 0x0C];
            _compressedFile.Read(entries, numEntries * 0x0C);

            for (int i = 0; i < numEntries; i++)
            {
                Span<byte> entry = entries.AsSpan(i * 0x0C);
                int fileNameOffset = BinaryPrimitives.ReadInt32LittleEndian(entry[0..]);
                int fileDataOffset = BinaryPrimitives.ReadInt32LittleEndian(entry[4..]);
                int fileSize = BinaryPrimitives.ReadInt32LittleEndian(entry[8..]);

                Files.Add(new PakFile()
                {
                    FileNameOffset = fileNameOffset,
                    FileDataOffset = fileDataOffset,
                    FileSize = fileSize
                });
            }

            byte[] fileNameSizeBuf = new byte[sizeof(int)];
            _compressedFile.Read(fileNameSizeBuf, 4);
            int fileNameBufferSize = BinaryPrimitives.ReadInt32LittleEndian(fileNameSizeBuf);

            byte[] fileNameBuffer = new byte[fileNameBufferSize];
            _compressedFile.Read(fileNameBuffer, fileNameBufferSize);

            // NOTE: Files must be ordered alphanumerically for bsearch.
            SpanReader fileNameReader = new SpanReader(fileNameBuffer);
            for (int i = 0; i < numEntries; i++)
            {
                var entry = Files[i];
                fileNameReader.Position = entry.FileNameOffset;
                entry.Name = fileNameReader.ReadString0();
            }
        }

        public void ExtractAll()
        {
            // Must extract in order for now
            // Otherwise implement compressed file seeking
            for (int i = 0; i < Files.Count; i++)
            {
                var entry = Files[i];

                byte[] file = new byte[entry.FileSize];
                _compressedFile.Read(file, entry.FileSize);

                string fullPath = Path.GetFullPath(entry.Name);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                File.WriteAllBytes(Files[i].Name, file);
            }
        }
    }
}
