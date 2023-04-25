using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wat.Extensions
{
    internal static class WatExtensions
    {
        public static int IndexOf<T>(this T[] array, T item) => Array.IndexOf(array, item);

        public static int ReadInt(this Stream s)
        {
            var bytes = new byte[4];

            s.Read(bytes, 0, 4);

            return BitConverter.ToInt32(bytes.ToArray(), 0);
        }

        public static string ReadString(this Stream s, int maxLength)
        {
            var bytes = new byte[maxLength];

            var bytesRead = s.Read(bytes, 0, maxLength);

            var length = Array.IndexOf(bytes, (byte)0);
            if (length < 0) length = maxLength;

            if (length < 0) return "";

            var chars = new char[length];

            for (var i = 0; i < length; i++)
                chars[i] = (char)bytes[i];

            return new string(chars.ToArray());
        }
    }
}
