namespace CharWidthMapTool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  Extract to text file  : CharSetTool -e input.map output.txt");
                Console.WriteLine("  Create from text file : CharSetTool -c input.txt output.map");
                Console.WriteLine("  Merge from text file  : CharSetTool -m input.map input.txt output.map");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            switch (args[0])
            {
                case "-e":
                {
                    var charSet = new CharSetFile();
                    charSet.Load(args[1]);
                    charSet.ExportAsText(args[2]);
                    break;
                }
                case "-c":
                {
                    var charSet = new CharSetFile();
                    charSet.AddFromTextFile(args[1]);
                    charSet.Save(args[2]);
                    break;
                }
                case "-m":
                {
                    if (args.Length < 4)
                    {
                        Console.WriteLine("ERROR: Requires 4 parameters.");
                        break;
                    }

                    var charSet = new CharSetFile();
                    charSet.Load(args[1]);
                    charSet.AddFromTextFile(args[2]);
                    charSet.Save(args[3]);
                    break;
                }
            }
        }
    }
}