using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text;
using System.Text.Json;

namespace AKBTool
{
    public class AKB
    {
        class Metadata
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public int OffsetX { get; set; }
            public int OffsetY { get; set; }
            public int BackgroundColor { get; set; }
            public string? BackgroundImage { get; set; }
        }

        public static void ExtractMetadata(string filePath)
        {
            using var reader = new BinaryReader(File.OpenRead(filePath));

            var magic = reader.ReadUInt32();

            if (magic != 0x20424B41 && magic != 0x2B424B41) // "AKB " && "AKB+"
                throw new Exception("Not a valid AKB image file.");

            var obj = new Metadata();

            obj.Width = reader.ReadUInt16();
            obj.Height = reader.ReadUInt16();

            reader.ReadUInt32(); // flags

            obj.BackgroundColor = reader.ReadInt32();

            obj.OffsetX = reader.ReadInt32(); // x0
            obj.OffsetY = reader.ReadInt32(); // y0

            reader.ReadUInt32(); // x1
            reader.ReadUInt32(); // y1

            if (magic == 0x2B424B41) // "AKB+"
            {
                var bytes = reader.ReadBytes(32)
                    .TakeWhile(x => x == 0)
                    .ToArray();

                obj.BackgroundImage = Encoding.GetEncoding(932)
                    .GetString(bytes);
            }

            var opts = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(obj, opts);
            var path = Path.ChangeExtension(filePath, ".metadata.json");

            File.WriteAllText(path, json);
        }

        public static void Create(string filePath, string sourcePath)
        {
            var source = Image.Load(sourcePath);

            if (source.PixelType.BitsPerPixel != 24 && source.PixelType.BitsPerPixel != 32)
                throw new Exception("Only 24-bit or 32-bit image are supported.");

            Metadata? metadata = null;

            try
            {
                var path = Path.ChangeExtension(sourcePath, ".metadata.json");
                var json = File.ReadAllText(path);
                metadata = JsonSerializer.Deserialize<Metadata>(json);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            if (metadata == null)
            {
                Console.WriteLine("WARNING: Failed to load metadata json, using default settings.");

                // Create default metadata object
                metadata = new Metadata
                {
                    Width = source.Width,
                    Height = source.Height,
                };
            }

            if (metadata.OffsetX + source.Width > metadata.Width)
            {
                Console.WriteLine("WARNING: Image width is expanded.");
                metadata.Width = metadata.OffsetX + source.Width;
            }

            if (metadata.OffsetY + source.Height > metadata.Height)
            {
                Console.WriteLine("WARNING: Image height is expanded.");
                metadata.Height = metadata.OffsetY + source.Height;
            }

            var bytesPerPixel = source.PixelType.BitsPerPixel / 8;
            var bytesPerRow = source.Width * bytesPerPixel;

            // Create pixel data buffer
            var pixels = new byte[source.Height * bytesPerRow];

            // Copy pixel data from image object to buffer
            if (source.PixelType.BitsPerPixel == 32)
                source.CloneAs<Bgra32>().CopyPixelDataTo(pixels);
            else
                source.CloneAs<Bgr24>().CopyPixelDataTo(pixels);

            // Stage 1 : Delta transform
            ApplyDelta(pixels, bytesPerPixel, bytesPerRow);

            // Stage 2 : Flip vertical
            if (source.PixelType.BitsPerPixel == 32)
            {
                var temp = Image.LoadPixelData<Bgra32>(pixels, source.Width, source.Height);
                temp.Mutate(op => op.Flip(FlipMode.Vertical));
                temp.CopyPixelDataTo(pixels);
            }
            else
            {
                var temp = Image.LoadPixelData<Bgr24>(pixels, source.Width, source.Height);
                temp.Mutate(op => op.Flip(FlipMode.Vertical));
                temp.CopyPixelDataTo(pixels);
            }

            // Stage 3 : Compression
            var comprPixels = new LzssCompressor().Compress(pixels, pixels.Length);

            // Stage 4 : Write
            using var writer = new BinaryWriter(File.Create(filePath));

            // Magic
            if (string.IsNullOrEmpty(metadata.BackgroundImage))
                writer.Write(new byte[] { 0x41, 0x4B, 0x42, 0x20 }); // "AKB "
            else
                writer.Write(new byte[] { 0x41, 0x4B, 0x42, 0x2B }); // "AKB+"

            // Canvas size
            writer.Write(Convert.ToUInt16(metadata.Width));
            writer.Write(Convert.ToUInt16(metadata.Height));

            // Flags
            if (source.PixelType.BitsPerPixel == 32)
                writer.Write(0x80000000);
            else
                writer.Write(0x400000FF);

            // Background color
            writer.Write(metadata.BackgroundColor);

            // Image rectangle
            writer.Write(metadata.OffsetX);
            writer.Write(metadata.OffsetY);
            writer.Write(metadata.OffsetX + source.Width);
            writer.Write(metadata.OffsetY + source.Height);

            // Pixel data
            writer.Write(comprPixels);

            // Background image
            if (!string.IsNullOrEmpty(metadata.BackgroundImage))
            {
                var buffer = new byte[32];
                var bytes = Encoding.GetEncoding(932).GetBytes(metadata.BackgroundImage);
                Array.Copy(bytes, buffer, bytes.Length);

                writer.Write(buffer);
            }

            // Done
            writer.Flush();
        }

        static void ApplyDelta(byte[] pixels, int pixel_size, int stride)
        {
            int i = pixels.Length - 1;
            for (int j = i - stride; j >= 0; i--, j--)
                pixels[i] -= pixels[j];
            i = stride - 1;
            for (int j = i - pixel_size; j >= 0; i--, j--)
                pixels[i] -= pixels[j];
        }
    }
}