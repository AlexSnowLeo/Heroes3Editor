using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Reflection;

namespace Heroes3Editor.Models
{
    public static class Extensions
    {
        public static int GetInt(this byte[] bytes, int startPosition)
        {
            return BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan().Slice(startPosition, 4));
        }

        public static string GetDescription<T>(this T source)
        {
            var fieldInfo = source.GetType().GetField(source.ToString());

            var attribute = fieldInfo.GetCustomAttribute<DescriptionAttribute>(false);

            return attribute != null
                ? attribute.Description
                : source.ToString();
        }
    }
}
