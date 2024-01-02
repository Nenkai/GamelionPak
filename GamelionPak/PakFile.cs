using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamelionPak
{
    public class PakFile
    {
        public string Name { get; set; }
        public int FileNameOffset;
        public int FileDataOffset;
        public int FileSize;

        public override string ToString()
        {
            return $"{Name} - NameOffset: 0x{FileNameOffset:X8}, DataOffset: 0x{FileDataOffset:X8} - Size:{FileSize:X8}";
        }
    }
}
