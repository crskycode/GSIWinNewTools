namespace StringMapTool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  Extract to text file  : StringMapTool -e input.map output.txt");
                Console.WriteLine("  Create from text file : StringMapTool -c input.txt output.map");
                Console.WriteLine("  Merge from text file  : StringMapTool -m input.map input.txt output.map");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            switch (args[0])
            {
                case "-e":
                {
                    var strMap = new StringMapFile();
                    strMap.Load(args[1]);
                    strMap.ExportText(args[2]);
                    break;
                }
                case "-c":
                {
                    var strMap = new StringMapFile();
                    strMap.ImportText(args[1], false);
                    strMap.Save(args[2]);
                    break;
                }
                case "-m":
                {
                    if (args.Length < 4)
                    {
                        Console.WriteLine("ERROR: Requires 4 parameters.");
                        break;
                    }

                    var strMap = new StringMapFile();
                    strMap.Load(args[1]);
                    strMap.ImportText(args[2], true);
                    strMap.Save(args[3]);
                    break;
                }
            }
        }
    }
}