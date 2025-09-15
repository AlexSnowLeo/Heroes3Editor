using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heroes3Editor.Models
{
    public static class Extensions
    {
        public static int GetInt(this byte[] bytes, int startPosition)
        {
            return BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan().Slice(startPosition, 4));
        }
    }
}
