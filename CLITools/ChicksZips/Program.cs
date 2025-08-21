using ChicksLib.Archives;

internal class Program
{
    public static void Main(string[] args)
    {
        string? sourcePath = null;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            if (sourcePath == null)
                sourcePath = arg;
        }

        if (sourcePath == null)
        {
            Console.WriteLine(@"Chick's Zips - a commandline tool that extracts the custom archives from Cars 3: Driven to Win
            
Usage: ChicksZips <archive path>
The tool always extracts files to the current working directory.");
            Environment.Exit(0);
        }

        using var fs = File.OpenRead(sourcePath);
        InfiniZip.ExtractZip(fs);
    }
}