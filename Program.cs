using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace FileMover
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Contains("--help"))
            {
                ShowHelp();
                return;
            }

            string targetDirectory = GetArgumentValue(args, "--target") ?? Directory.GetCurrentDirectory();
            bool askToProceed = !args.Contains("--execute");

            if (!Directory.Exists(targetDirectory))
            {
                Console.WriteLine($"Target directory does not exist: {targetDirectory}");
                return;
            }

            string[] jsonFiles = Directory.GetFiles(targetDirectory, "*.cm-info.json");
            Dictionary<string, List<string>> filesToMove = new Dictionary<string, List<string>>();

            foreach (var jsonFile in jsonFiles)
            {
                try
                {
                    Console.WriteLine($"Processing file: {jsonFile}");

                    string jsonContent = File.ReadAllText(jsonFile);
                    JObject json = JObject.Parse(jsonContent);

                    string baseModel = json["BaseModel"]?.ToString();
                    if (string.IsNullOrEmpty(baseModel))
                    {
                        Console.WriteLine($"BaseModel not found in JSON file: {jsonFile}");
                        continue;
                    }

                    if (!filesToMove.ContainsKey(baseModel))
                    {
                        filesToMove[baseModel] = new List<string>();
                    }

                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(jsonFile).Replace(".cm-info", "");

                    string[] extensions = { "cm-info.json", "preview.jpeg", "safetensors" };

                    foreach (var extension in extensions)
                    {
                        string fileToMove = $"{fileNameWithoutExtension}.{extension}";
                        string fullFilePath = Path.Combine(targetDirectory, fileToMove);

                        if (File.Exists(fullFilePath))
                        {
                            filesToMove[baseModel].Add(fullFilePath);
                        }
                        else
                        {
                            Console.WriteLine($"File not found: {fullFilePath}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file: {jsonFile}");
                    Console.WriteLine(ex.Message);
                }
            }

            if (askToProceed)
            {
                // Display stats and ask for confirmation
                Console.WriteLine("\nFiles to be moved, grouped by BaseModel:");
                foreach (var entry in filesToMove)
                {
                    Console.WriteLine($"BaseModel: {entry.Key}");
                    foreach (var file in entry.Value)
                    {
                        Console.WriteLine($"  {file}");
                    }
                }

                Console.WriteLine("\nDo you want to proceed with moving these files? (yes/no)");
                string response = Console.ReadLine();

                if (response?.ToLower() != "yes")
                {
                    Console.WriteLine("Operation aborted by user.");
                    return;
                }
            }

            // Proceed with moving files
            foreach (var entry in filesToMove)
            {
                string baseModel = entry.Key;
                string modelDir = Path.Combine(targetDirectory, baseModel);

                if (!Directory.Exists(modelDir))
                {
                    try
                    {
                        Directory.CreateDirectory(modelDir);
                        Console.WriteLine($"Created directory: {modelDir}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error creating directory: {modelDir}");
                        Console.WriteLine(ex.Message);
                        continue;
                    }
                }

                foreach (var filePath in entry.Value)
                {
                    try
                    {
                        string fileName = Path.GetFileName(filePath);
                        string destinationPath = Path.Combine(modelDir, fileName);
                        File.Move(filePath, destinationPath);
                        Console.WriteLine($"Moved file: {fileName} to {modelDir}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error moving file: {filePath}");
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            Console.WriteLine("File move operation completed.");
        }

        static void ShowHelp()
        {
            Console.WriteLine("Usage: FileMover [OPTIONS]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --help           Show this help message and exit.");
            Console.WriteLine("  --target PATH    Specify the target directory to process. If not specified, the current directory will be used.");
            Console.WriteLine("  --execute        Execute the file move without asking for confirmation.");
            Console.WriteLine("\nDescription:");
            Console.WriteLine("This tool moves files based on JSON metadata in the specified or current directory.");
            Console.WriteLine("It reads *.cm-info.json files, creates model directories based on the");
            Console.WriteLine("BaseModel element, and moves corresponding files (.cm-info.json, .preview.jpeg,");
            Console.WriteLine(".safetensors) to the respective directories.");
        }

        static string GetArgumentValue(string[] args, string option)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == option && i + 1 < args.Length)
                {
                    return args[i + 1];
                }
            }
            return null;
        }
    }
}
