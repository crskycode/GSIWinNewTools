using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CharWidthMapTool
{
    internal class CharSetFile
    {
        readonly SortedSet<char> _charSet = new();

        public void Load(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var reader = new BinaryReader(stream);

            var count = reader.ReadUInt16();

            if (count * 2 + 2 != stream.Length)
            {
                throw new Exception("Invalid character width map file.");
            }

            _charSet.Clear();

            for (var i = 0; i < count; i++)
            {
                _charSet.Add(Convert.ToChar(reader.ReadUInt16()));
            }
        }

        public void Save(string filePath)
        {
            if (_charSet.Count > ushort.MaxValue)
            {
                Console.WriteLine("WARNING: Too many characters in the map.");
            }

            using var stream = File.Create(filePath);
            using var writer = new BinaryWriter(stream);

            var count = Math.Min(_charSet.Count, ushort.MaxValue);

            writer.Write(Convert.ToUInt16(count));

            foreach (var e in _charSet)
            {
                writer.Write(Convert.ToUInt16(e));

                if (--count <= 0)
                {
                    break;
                }
            }

            writer.Flush();
        }

        public void ExportAsText(string filePath)
        {
            using var writer = File.CreateText(filePath);

            var count = 0;

            foreach (var e in _charSet)
            {
                writer.Write(e);

                if (++count == 20)
                {
                    writer.WriteLine();
                    count = 0;
                }
            }

            writer.Flush();
        }

        public void AddFromString(string sequence)
        {
            foreach (var e in sequence)
            {
                if (e != '\r' && e != '\n')
                {
                    _charSet.Add(e);
                }
            }
        }

        public void AddFromTextFile(string filePath)
        {
            string text = File.ReadAllText(filePath);
            AddFromString(text);
        }

        public void Clear()
        {
            _charSet.Clear();
        }
    }
}
