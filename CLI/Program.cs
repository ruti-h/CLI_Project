using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
#region optoin & alias
var rootCommand = new RootCommand("CLI Tool for bundling code files");
var bundle = new Command("bundle", "Bundles multiple code files into one file");
var languageOption = new Option<string>("--language", "Specify the programming languages to include (e.g., cs, js). Use 'all' to include all languages.")
{
    IsRequired = true 
};
bundle.AddOption(languageOption);
languageOption.AddAlias("--l");
var outputOption = new Option<FileInfo>("--output", "Specify the output file path or name.");
bundle.AddOption(outputOption);
outputOption.AddAlias("--o");
var noteOption = new Option<bool>("--note", "Include the source file path as a comment in the bundled file.");
bundle.AddOption(noteOption);
noteOption.AddAlias("--n");
var sortOption = new Option<string>("--sort", description: "How to sort", getDefaultValue: () => "abc");
sortOption.AddAlias("--s");
bundle.AddOption(sortOption);
var removeEmptyLinesOption = new Option<bool>("--remove-empty-lines", "Remove empty lines from the bundled file.");
bundle.AddOption(removeEmptyLinesOption);
removeEmptyLinesOption.AddAlias("--r");
var authorOption = new Option<string>("--author", "Include the author's name in the bundled file as a comment.");
bundle.AddOption(authorOption);
authorOption.AddAlias("--a");
#endregion
#region rsp
var createRsp = new Command("create-rsp", "Generates a response file with the command for bundling code files");
createRsp.SetHandler(() =>
{
    try
    {
        string res = "";
        var responseFile = new FileInfo("responseFile.rsp");
        Console.WriteLine("Enter values for the bundle command:");
        using (StreamWriter rspWriter = new StreamWriter(responseFile.FullName))
        {
            Console.Write("Output file path: ");
            var Output = Console.ReadLine();
            while (string.IsNullOrWhiteSpace(Output))
            {
                Console.Write("Enter the output file path: ");
                Output = Console.ReadLine();
            }
            res += $"--o {Output} ";
            Console.Write("Languages (comma-separated): ");
            var languages = Console.ReadLine();
            while (string.IsNullOrWhiteSpace(languages))
            {
                Console.Write("Please enter at least one programming language: ");
                languages = Console.ReadLine();
            }
            res += $"--l {languages} ";
            Console.Write("Add note (y/n): ");
            string note = Console.ReadLine().Trim().ToLower() == "y" ? "--n true " : "--n false ";
            res += note;
            Console.Write("Sort by (abc or language): ");
            res += $"--s {Console.ReadLine()} ";
            Console.Write("Remove empty lines (y/n): ");
            res += Console.ReadLine().Trim().ToLower() == "y" ? "--r true " : "--r false ";
            Console.Write("Author: ");
            res += $"--a {Console.ReadLine()} ";
            rspWriter.WriteLine(res);
        }
        Console.WriteLine("Response file created successfully: " + responseFile.FullName);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error creating response file: " + ex.Message);
    }
});
#endregion
bundle.SetHandler((string language, FileInfo output, bool note, string sort, bool removeEmptyLines, string author) =>
{
    List<string> extensionList = new List<string>();
    try
    {
        if (language.Contains("all"))
        {
            extensionList.Add(".cs");
            extensionList.Add(".java");
            extensionList.Add(".py");
            extensionList.Add(".js");
            extensionList.Add(".rb");
            extensionList.Add(".cpp");
            extensionList.Add(".h");
            BundleFiles(output, note, sort, removeEmptyLines, author, extensionList); // קריאה לפונקציה המאוחדת
        }
        else
        {
            List<string> selectedLanguage = language.Split(',').ToList();

            List<string> temp = new List<string>();
            for (int i = 0; i < selectedLanguage.Count(); i++)
            {
                temp = GetFileExtension(selectedLanguage[i]);
                if (temp != null && temp.Count > 0) 
                {
                    extensionList.AddRange(temp); 
                }
                else
                {
                    Console.WriteLine($"The {selectedLanguage[i]} language is invalid");
                }
            }
            if (extensionList.Count() != 0)
            {
                BundleFiles(output, note, sort, removeEmptyLines, author, extensionList); 
            }
        }
    }
    catch (IOException ex)
    {
        Console.WriteLine($"Error in bundle command: {ex.Message}");
    }
}, languageOption, outputOption, noteOption, sortOption, removeEmptyLinesOption, authorOption);
#region functions
static List<string> GetFileExtension(string language) 
{
    return language switch
    {
        "CSharp" => new List<string> { "cs" },
        "Java" => new List<string> { "java" },
        "Python" => new List<string> { "py" },
        "JavaScript" => new List<string> { "js" },
        "CPlusPlus" => new List<string> { "cpp", "h" },
        "TypeScript" => new List<string> { "ts" }, 
        _ => new List<string>(), 
    };

}
static void BundleFiles(FileInfo output, bool note, string sort, bool removeEmptyLines, string author, List<string> extensions/*,string allOrSpecific = "specific"*/)
{
    List<string> excludedDirectories = new List<string>
    {
        "Debug",
        "public",
        "node_modules",
        "Lib", ".idea",
        ".itynb_checkpoints",
        "bin",
        "obj",
        "publish",
        "Migrations",
        "test",
        ".git"
    };
    try
    {
        string path = output?.FullName;
        if (string.IsNullOrEmpty(path))
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), "bundleFile.txt");
        }
        var directoryToSearch = output?.DirectoryName ?? Directory.GetCurrentDirectory();
        List<string> filesToBundle = new List<string>();

        foreach (var file in Directory.GetFiles(directoryToSearch, "*.*", SearchOption.AllDirectories))
        {
            if (!excludedDirectories.Any(dir => file.Contains(dir)) &&
                extensions.Any(ext => file.EndsWith(ext.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                filesToBundle.Add(file);
            }
        }

        if (filesToBundle.Count == 0)
        {
            Console.WriteLine("No files found matching the specified criteria.");
            return;
        }
        Console.WriteLine($"Found {filesToBundle.Count} files to bundle.");
        filesToBundle = SortFiles(filesToBundle, "abc").ToList();
        using (StreamWriter writer = new StreamWriter(path, append: true))
        {
            foreach (var file in filesToBundle)
            {
                try
                {
                    if (author != null)

                    {
                        writer.WriteLine("//" + author + "\n");
                    }
                    // הוספת הערה עם נתיב הקובץ, אם נדרש
                    if (note)
                    {
                        string relativePath = Path.GetRelativePath(directoryToSearch, file); // קבלת נתיב יחסי
                        writer.WriteLine($"// {path}// {relativePath}");
                    }
                    if (removeEmptyLines)
                        RemoveEmptyLines(file);
                    // כתיבת התוכן לקובץ הפלט
                    writer.WriteLine(File.ReadAllText(file));
                    writer.WriteLine(); // רווח בין הקבצים
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {file}: {ex.Message}");
                }
            }
        }
        Console.WriteLine($"All files have been bundled into {path}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }
}
static string[] SortFiles(List<string> files, string sortOrder = "abc")
{
    if (sortOrder == "abc")
        return files.OrderBy(f => Path.GetFileName(f)).ToArray(); // מיין לפי שם הקובץ
    else if (sortOrder == "language")
        return files.OrderBy(f => Path.GetExtension(f)).ToArray(); // מיין לפי סוג הקובץ
    else
        throw new ArgumentOutOfRangeException(nameof(sortOrder), sortOrder, "Invalid sort order"); // זרוק חריגה אם יש ערך לא מוכר
}
static string RemoveEmptyLines(string filePath)
{
    try
    {
        var lines = File.ReadAllLines(filePath);
        var nonEmptyLines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
        File.WriteAllLines(filePath, nonEmptyLines);
        return filePath;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error removing empty lines from {filePath}: {ex.Message}");
        return filePath; 
    }
}
#endregion
rootCommand.AddCommand(bundle);
rootCommand.AddCommand(createRsp);
return await rootCommand.InvokeAsync(args);
