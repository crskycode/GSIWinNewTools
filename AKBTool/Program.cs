namespace AKBTool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  Extract metadata file : AKBTool -e image.akb");
                Console.WriteLine("  Create AKB image file : AKBTool -c image.png image.akb");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            switch (args[0])
            {
                case "-e":
                {
                    AKB.ExtractMetadata(args[1]);
                    break;
                }
                case "-c":
                {
                    if (args.Length < 3)
                    {
                        Console.WriteLine("ERROR: Required 3 arguments.");
                        break;
                    }
                    AKB.Create(args[2], args[1]);
                    break;
                }
            }
        }
    }
}