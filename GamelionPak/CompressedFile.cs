using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

using ManagedLzma.LZMA;

using SevenZip.Sdk.Compression;

namespace GamelionPak
{
    public class CompressedFile
    {
        // Reader
        private BinaryReader _reader;
        private byte[] _wholeFile;
        private ManagedLzma.LZMA.Decoder _decoder;

        // Chunk state
        private byte[] _currentChunk;
        private int _currentChunkIndex = -1;
        private int _chunkOffset;
        private int _globalOffset;
        private bool _chunkIsCompressed;
        private int _currentChunkSize;

        // In header
        public int StartChunkedOffset;
        public byte[] LZMAProperties;
        public int ChunkSize { get; set; }
        public int[] ChunkSizes;

        public CompressedFile()
        {

        }

        // Claw::CompressedFile::Open - 0x2244D0
        public void Open(string file)
        {
            _wholeFile = File.ReadAllBytes(file);
            var fs = new FileStream(file, FileMode.Open);
            _reader = new BinaryReader(fs);

            StartChunkedOffset = _reader.ReadInt32();
            ChunkSize = _reader.ReadInt32();
            int chunkListSize = _reader.ReadInt32(); // if this under 0, read raw instead?

            LZMAProperties = new byte[5];
            fs.Read(LZMAProperties, 0, 5);
            _decoder = new ManagedLzma.LZMA.Decoder(DecoderSettings.ReadFrom(LZMAProperties, 0));

            byte[] chunkOffsets = DecompressDirect(_reader, chunkListSize);
            ChunkSizes = new int[chunkListSize / 4];
            for (int i = 0; i < ChunkSizes.Length; i++)
                ChunkSizes[i] = BinaryPrimitives.ReadInt32LittleEndian(chunkOffsets.AsSpan(i * sizeof(int)));

        }

        // lzma decompress function? 0x224CA0
        private byte[] DecompressDirect(BinaryReader reader, int size)
        {
            int rem = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
            int numInputRead = _decoder.Decode(_wholeFile, (int)reader.BaseStream.Position, rem, size, false);

            byte[] outputBuf = new byte[size];
            int numOutputRead = _decoder.ReadOutputData(outputBuf, 0, size);

            _reader.BaseStream.Position += numInputRead;
            return outputBuf;
        }

        // CompressedFile::Read - 0x2248D0
        public Span<byte> Read(byte[] output, int numRequested)
        {
            int outputOffset = 0;

            while (numRequested > 0)
            {
                int chunkIndex = _globalOffset / ChunkSize;
                int remToNextChunk = _globalOffset % ChunkSize;

                if (chunkIndex != _currentChunkIndex || _chunkOffset >= ChunkSize)
                {
                    int prev = chunkIndex > 0 ? (int)(ChunkSizes[chunkIndex - 1] & 0x7FFFFFFF) : 0;
                    int cur = ChunkSizes[chunkIndex];

                    _currentChunkSize = (int)((cur - prev) & 0x7FFFFFF);
                    _currentChunk = new byte[_currentChunkSize];
                    _reader.BaseStream.Position = prev + StartChunkedOffset;
                    _reader.Read(_currentChunk);

                    _chunkIsCompressed = (cur & 0x80000000) != 0;

                    if (_chunkIsCompressed)
                    {
                        _decoder = new ManagedLzma.LZMA.Decoder(DecoderSettings.ReadFrom(LZMAProperties, 0));
                        int numInputRead = _decoder.Decode(_currentChunk, 0, _currentChunkSize, ChunkSize, false);
                    }

                    _currentChunkIndex = chunkIndex;
                    _chunkOffset = 0;
                    
                }

                int v20 = ChunkSize - remToNextChunk;
                int bytesRead = Math.Min(numRequested, v20);

                if (_chunkIsCompressed)
                {
                    bytesRead = _decoder.ReadOutputData(output, outputOffset, bytesRead);
                    _chunkOffset += bytesRead;

                    if (bytesRead <= 0)
                        return output;
                }
                else
                {
                    // basically memcpy
                    _currentChunk.AsSpan(_chunkOffset, bytesRead).CopyTo(output.AsSpan(outputOffset, bytesRead));
                    _chunkOffset += bytesRead;
                }

                numRequested -= bytesRead;
                _globalOffset += bytesRead;
                outputOffset += bytesRead;
            }

            return output;
        }

        public void Seek(int globalOffset)
        {
            // TODO
        }
    }
}
