using System.Text;

namespace StringMapTool
{
    internal static class Extensions
    {
        internal static string ReadNullTerminatedUnicodeString(this BinaryReader reader, int capacity)
        {
            var sb = new StringBuilder(capacity);

            while (true)
            {
                var c = (char)reader.ReadUInt16();

                if (c == 0)
                {
                    break;
                }

                sb.Append(c);
            }

            if (sb.Length == 0)
            {
                return string.Empty;
            }

            return sb.ToString();
        }

        internal static void WriteNullTerminatedUnicodeString(this BinaryWriter writer, string s)
        {
            writer.Write(Encoding.Unicode.GetBytes(s));
            writer.Write((short)0);
        }
    }
}
