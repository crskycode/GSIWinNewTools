using System.Globalization;
using System.Text.RegularExpressions;

namespace StringMapTool
{
    internal class StringMapFile
    {
        readonly SortedDictionary<uint, string> _stringMap = new();

        public void Load(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var reader = new BinaryReader(stream);

            var count = reader.ReadInt32();

            var indexSize = count * 8;

            if (indexSize + 4 > stream.Length)
            {
                throw new Exception("Invalid string map file.");
            }

            var indexData = reader.ReadBytes(indexSize);

            if (indexData.Length != indexSize)
            {
                throw new Exception("Invalid string map file.");
            }

            var poolStartPos = 4 + indexSize;
            var poolEndPos = stream.Length;

            long totalSize = poolStartPos;

            for (var i = 0; i < count; i++)
            {
                var key = BitConverter.ToUInt32(indexData, i * 8);
                var offset = BitConverter.ToInt32(indexData, i * 8 + 4);

                if (offset < poolStartPos || offset >= poolEndPos)
                {
                    throw new Exception("Broken string map file.");
                }

                stream.Position = offset;
                var str = reader.ReadNullTerminatedUnicodeString(256);

                totalSize += (stream.Position - offset);

                if (!_stringMap.TryAdd(key, str))
                {
                    Console.WriteLine("WARNING: Duplicate string key.");
                }
            }

            if (totalSize != stream.Length)
            {
                Console.WriteLine("WARNING: The file has not been fully read.");
            }
        }

        public void Save(string filePath)
        {
            using var stream = File.Create(filePath);
            using var writer = new BinaryWriter(stream);

            var count = _stringMap.Count;

            writer.Write(count);

            var _posMap = new Dictionary<uint, TEntry>(count);

            foreach (var e in _stringMap)
            {
                _posMap.Add(e.Key, new TEntry { Pos1 = stream.Position });
                writer.Write(e.Key);
                writer.Write(0);
            }

            foreach (var e in _stringMap)
            {
                _posMap[e.Key].Pos2 = stream.Position;
                writer.WriteNullTerminatedUnicodeString(e.Value);
            }

            foreach (var e in _posMap)
            {
                stream.Position = e.Value.Pos1 + 4;
                writer.Write(Convert.ToUInt32(e.Value.Pos2));
            }

            writer.Flush();
        }

        class TEntry
        {
            public long Pos1;
            public long Pos2;
        }

        public void ExportText(string filePath)
        {
            using var writer = File.CreateText(filePath);

            foreach (var e in _stringMap)
            {
                var s = e.Value;

                writer.WriteLine($"◇{e.Key:X8}◇{s}");
                writer.WriteLine($"◆{e.Key:X8}◆{s}");
                writer.WriteLine();
            }

            writer.Flush();
        }

        public void ImportText(string filePath, bool merge)
        {
            if (!merge)
            {
                _stringMap.Clear();
            }

            using var reader = File.OpenText(filePath);

            var lineNo = 0;

            while (!reader.EndOfStream)
            {
                var ln = lineNo;
                var line = reader.ReadLine();
                lineNo++;

                if (line == null)
                    break;

                if (line.Length == 0 || line[0] != '◆')
                    continue;

                var m = Regex.Match(line, @"◆(\w+)◆(.+$)");

                if (!m.Success || m.Groups.Count != 3)
                    throw new Exception($"Bad format at line: {ln}");

                var key = uint.Parse(m.Groups[1].Value, NumberStyles.HexNumber);
                var str = m.Groups[2].Value;

                if (merge)
                {
                    if (!_stringMap.ContainsKey(key))
                    {
                        throw new Exception($"Invalid key at line: {ln}");
                    }

                    _stringMap[key] = str;
                }
                else
                {
                    _stringMap.Add(key, str);
                }
            }
        }

        static string EscapeString(string input)
        {
            input = input.Replace("\n", "\\n");
            input = input.Replace("\r", "\\r");
            input = input.Replace("\t", "\\t");

            return input;
        }

        static string UnescapeString(string input)
        {
            input = input.Replace("\\n", "\n");
            input = input.Replace("\\r", "\r");
            input = input.Replace("\\t", "\t");

            return input;
        }
    }
}
