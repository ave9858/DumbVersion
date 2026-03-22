using LibDumbVersion;

namespace DumbVersionPatcher;

internal class Program
{
    static void Main(string[] args)
    {
        Console.Title = "DumbVersion Patcher";
        Console.WriteLine("DumbVersion Patcher");
        Console.WriteLine(@" /\_/\  ");
        Console.WriteLine(@"( o.o ) ");
        Console.WriteLine(@" > ^ <");

        if (args.Contains("-h") || args.Contains("-?") || args.Contains("--help"))
        {
            PrintHelp();
            return;
        }
        
        List<string> fileArgs = new();
        string outputDest = "";

        for (int i = 0; i < args.Length; i++) {
            if ((args[i] == "-o" ||  args[i] == "--output") && (i < args.Length - 1))
            {
                outputDest = args[++i];
            }
            else
            {
                fileArgs.Add(args[i]);
            }
        }

        if (args.Length == 0)
        {
            var dir = AppContext.BaseDirectory;
            var patchFiles = Directory.EnumerateFiles(dir, "*.dvp").ToList();

            if (patchFiles.Count == 0)
            {
                Console.WriteLine("No patch files found in the current directory.");
            }
            else
            {
                string selectedPatch = patchFiles[0];

                if (patchFiles.Count > 1)
                {
                    if (!Console.IsOutputRedirected)
                    {
                        Console.WriteLine("Multiple patch files found. Which one do you want to apply?");

                        for (int i = 0; i < patchFiles.Count; i++)
                            Console.WriteLine($"[{i + 1}] {Path.GetFileName(patchFiles[i])}");

                        Console.Write("> ");
                        if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= patchFiles.Count)
                        {
                            selectedPatch = patchFiles[choice - 1];
                        }
                        else
                        {
                            Console.WriteLine("Invalid selection");
                            selectedPatch = "";
                        }
                    }
                    else
                    {
                        Console.WriteLine("Multiple patch files found. Please specify the patch file to be used as a command-line argument.");
                        selectedPatch = "";
                    }
                }

                if (!string.IsNullOrEmpty(selectedPatch))
                {
                    ProcessPatch(selectedPatch, "", "", false);
                }
            }
        }
        else
        {
            string isoSrc = "";

            if (!fileArgs[0].EndsWith(".dvp", StringComparison.OrdinalIgnoreCase))
            {
                isoSrc = fileArgs[0];
            }

            var patchArgs = fileArgs.Where(x => x.EndsWith(".dvp", StringComparison.OrdinalIgnoreCase)).ToList();

            if (!patchArgs.Any())
            {
                Console.WriteLine("No patch files given.");
            }
            else
            {
                bool destAsFile = patchArgs.Count == 1;

                foreach (var patchFile in patchArgs)
                {
                    Console.WriteLine($"Processing {patchFile}...");
                    ProcessPatch(patchFile, isoSrc, outputDest, destAsFile);
                }
            }
        }

        EnterToExit();
    }

    private static void ProcessPatch(string patchFile, string isoSrc, string outputDest, bool destAsFile)
    {
        var patchDir = Path.GetDirectoryName(Path.GetFullPath(patchFile));
        var targetExt = "";

        if (string.IsNullOrEmpty(patchDir))
        {
            patchDir = AppContext.BaseDirectory;
        }

        List<string> isoFiles;

        if (string.IsNullOrEmpty(isoSrc))
        {
            using var patch = new PatchFile(patchFile, write: false);
            targetExt = Path.GetExtension(patch.BaseFileName);
            var isoPath = Path.Combine(patchDir, patch.BaseFileName);

            if (File.Exists(isoPath))
            {
                isoSrc = isoPath;
                isoFiles = [isoSrc];
            }
            else
            {
                Console.WriteLine($"Base file {patch.BaseFileName} was not found in the same directory as {patchFile}.");
                isoFiles = Directory.EnumerateFiles(patchDir, "*" + targetExt).ToList();
            }
        }
        else
        {
            targetExt = Path.GetExtension(isoSrc);
            isoFiles = [isoSrc];
        }

        if (isoFiles.Count == 0)
        {
            Console.WriteLine($"No base file(s) found in the same directory as {patchFile}.");
            return;
        }

        string selectedIso = isoFiles[0];

        if (!Console.IsOutputRedirected)
        {
            if (isoFiles.Count > 1)
            {
                Console.WriteLine("\nMultiple base files found. Which one do you want to use?");

                for (int i = 0; i < isoFiles.Count; i++)
                    Console.WriteLine($"[{i + 1}] {Path.GetFileName(isoFiles[i])}");

                Console.Write("> ");
                if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= isoFiles.Count)
                {
                    selectedIso = isoFiles[choice - 1];
                }
                else
                {
                    Console.WriteLine("Invalid selection.");
                    return;
                }
            }
        }
        else
        {
            if (isoFiles.Count > 1)
            {
                Console.WriteLine("Multiple base files found. Please specify the base file to be used as a command-line argument.");
                return;
            }
        }
        
        bool hasOutputDest = !string.IsNullOrEmpty(outputDest);
        string targetIsoName = Path.GetFileNameWithoutExtension(patchFile) + targetExt;
        string targetDir = (destAsFile || !hasOutputDest) ? (patchDir ?? "") : outputDest;

        if (!Path.Exists(targetDir))
        {
            Console.WriteLine($"Output directory {targetDir} does not exist.\n");
            return;
        }

        string targetIsoPath = (destAsFile && hasOutputDest) ? outputDest : 
            string.IsNullOrEmpty(targetDir) ? targetIsoName : Path.Combine(targetDir, targetIsoName);

        if (targetIsoPath.Equals(selectedIso, StringComparison.OrdinalIgnoreCase))
        {
            targetIsoPath = targetIsoPath.Replace(targetExt, "_patched" + targetExt);
        }

        if (File.Exists(targetIsoPath)) {
            Console.Write($"File {targetIsoPath} already exists. Overwrite this file? [Y/N] ");
            var key = Console.ReadKey();

            if (key.Key == ConsoleKey.N)
            {
                Console.WriteLine("\nSkipping this file.\n");
                return;
            }
        }

        try
        {
            DiffEngine.ApplyPatch(selectedIso, patchFile, targetIsoPath, DrawProgressBar);
            Console.WriteLine("\n\nFile patched successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");

            if (File.Exists(targetIsoPath))
            {
                Console.WriteLine("Cleaning up incomplete output file...");
                try { File.Delete(targetIsoPath); } catch { /* ignored */ }
            }
        }

        Console.WriteLine();
    }

    private static void PrintHelp()
    {
        string progFn = Path.GetFileName(Environment.GetCommandLineArgs()[0]);
        Console.WriteLine("Usage:");
        Console.WriteLine($"{progFn} [-o/--output output_path] [base_file] [patch1.dvp, patch2.dvp ...]");
        Console.WriteLine("-o/--output        Output filename for single patch file, output directory for multiple patch files");
        Console.WriteLine("\nNotes:");
        Console.WriteLine("If no arguments are given, .dvp files will be searched for in the folder this program is located in.");
        Console.WriteLine("If base file is not given, it will be searched for in the same directory as the .dvp file.");
        Console.WriteLine("If this base file is not found, the directory of the patch file will be scanned for all .iso files.");
        Console.WriteLine("If multiple applicable .dvp or base files are found, a menu will be shown to select the correct files.");
    }

    private static void DrawProgressBar(int progress)
    {
        if (Console.IsOutputRedirected) return; // in case the patcher runs somewhere where stdout is restricted (docker or something)
        Console.CursorLeft = 0;
        Console.Write("[");
        int filled = progress / 2;
        Console.Write(new string('█', filled));
        Console.Write(new string('░', 50 - filled));
        Console.Write($"] {progress}% ");
    }

    private static void EnterToExit()
    {
        if (Console.IsOutputRedirected) return;
        Console.WriteLine("Press Enter to exit.");
        Console.ReadLine();
    }
}