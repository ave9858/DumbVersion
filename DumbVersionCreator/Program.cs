using System.Diagnostics;
using LibDumbVersion;

namespace DumbVersionCreator;

internal class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            PrintUsage();
            return;
        }

        if (args[0].Equals("-bulk", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length < 3)
            {
                PrintUsage();
                return;
            }

            string baseIsoFile = args[1];
            string targetFolder = args[2];
            string outputFolder = args.Length >= 4 ? args[3] : targetFolder;

            RunBulkMode(baseIsoFile, targetFolder, outputFolder);
        }
        else
        {
            string baseIsoFile = args[0];
            string targetIsoFile = args[1];
            string patchFile;

            if (args.Length < 3)
                patchFile =
                    Path.Combine(Path.GetDirectoryName(targetIsoFile)!,
                    Path.GetFileNameWithoutExtension(targetIsoFile) + ".dvp");
            else
                patchFile = args[2];

            try
            {
                DiffEngine.CreatePatch(baseIsoFile, targetIsoFile, patchFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
                if (File.Exists(patchFile))
                {
                    try { File.Delete(patchFile); } catch { /* ignored */ }
                }
            }
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Single file mode:");
        Console.WriteLine("  DumbVersionCreator <base_file> <target_file> [output.dvp]");
        Console.WriteLine("\nBulk mode:");
        Console.WriteLine("  DumbVersionCreator -bulk <base_file> <target_folder> [output_folder]");
    }

    private static void RunBulkMode(string baseIsoFile, string targetFolder, string outputFolder)
    {
        try
        {
            if (!File.Exists(baseIsoFile))
            {
                Console.WriteLine($"Base file not found: {baseIsoFile}");
                return;
            }

            if (!Directory.Exists(targetFolder))
            {
                Console.WriteLine($"Target folder does not exist: {targetFolder}");
                return;
            }

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            var baseExt = Path.GetExtension(baseIsoFile);
            var targetFiles = Directory.EnumerateFiles(targetFolder, "*" + baseExt).ToList();
            if (targetFiles.Count == 0)
            {
                Console.WriteLine($"No base files found in {targetFolder}");
                return;
            }

            Console.WriteLine("Indexing base...");
            Stopwatch stopwatch = Stopwatch.StartNew();
            using var baseIndex = new BaseFileIndex(baseIsoFile);
            Console.WriteLine($"Base file indexed: {baseIndex.RecordCount} unique chunks");
            Console.WriteLine($"Took {stopwatch.Elapsed.TotalSeconds:0.00}s\n");

            foreach (var targetIsoFile in targetFiles)
            {
                if (Path.GetFullPath(targetIsoFile).Equals(Path.GetFullPath(baseIsoFile), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string patchFile = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(targetIsoFile) + ".dvp");

                Console.WriteLine($"Processing {Path.GetFileName(targetIsoFile)}");
                try
                {
                    DiffEngine.CreatePatch(baseIndex, baseIsoFile, targetIsoFile, patchFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to process {targetIsoFile}: {ex.Message}");
                    if (File.Exists(patchFile))
                    {
                        try { File.Delete(patchFile); } catch { /* ignored */ }
                    }
                }

                Console.WriteLine();
            }

            Console.WriteLine("Bulk generation completed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during bulk processing: {ex.Message}");
        }
    }
}