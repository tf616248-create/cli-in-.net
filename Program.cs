using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // --language חובה
        var languageOption = new Option<string>(
            "--language",
            "רשימת שפות תכנות (מופרדות בפסיקים), או 'all' לכל הקבצים")
        {
            IsRequired = true
        };
        languageOption.AddAlias("-l");

        // --output
        var outputOption = new Option<string>(
            "--output",
            "שם הקובץ המיוצא או נתיב מלא (אם יש רווחים, עטוף במרכאות)")
        {
            IsRequired = false
        };
        outputOption.AddAlias("-o");

        // --note
        var noteOption = new Option<bool>(
            "--note",
            "לרשום את מקור הקוד כהערה בקובץ ה-bundle")
        {
            IsRequired = false
        };
        noteOption.AddAlias("-n");

        // --sort
        var sortOption = new Option<string>(
            "--sort",
            "סדר העתקת הקבצים ('name' או 'type')")
        {
            IsRequired = false
        };
        sortOption.SetDefaultValue("name");
        sortOption.AddAlias("-s");

        // --remove-empty-lines
        var removeEmptyLinesOption = new Option<bool>(
            "--remove-empty-lines",
            "למחוק שורות ריקות מהקוד")
        {
            IsRequired = false
        };
        removeEmptyLinesOption.AddAlias("-r");

        // --author
        var authorOption = new Option<string>(
            "--author",
            "שם יוצר הקובץ")
        {
            IsRequired = false
        };
        authorOption.AddAlias("-a");

        // פקודה bundle
        var bundleCommand = new Command("bundle", "אורז קבצי קוד לקובץ אחד")
        {
            languageOption,
            outputOption,
            noteOption,
            sortOption,
            removeEmptyLinesOption,
            authorOption
        };

        bundleCommand.SetHandler(
            (string language, string output, bool note, string sort, bool removeEmptyLines, string author) =>
            {
                HandleBundle(language, output, note, sort, removeEmptyLines, author);
            },
            languageOption, outputOption, noteOption, sortOption, removeEmptyLinesOption, authorOption
        );

        // פקודה create-rsp
        var createRspCommand = new Command("create-rsp", "יוצר קובץ תגובה עם פקודה מוכנה");
        createRspCommand.SetHandler(async () => { await CreateResponseFile(); });

        // Root command
        var rootCommand = new RootCommand("CLI לאריזת קבצי קוד");
        rootCommand.AddCommand(bundleCommand);
        rootCommand.AddCommand(createRspCommand);

        return await rootCommand.InvokeAsync(args);
    }

    static void HandleBundle(string language, string output, bool note, string sort, bool removeEmptyLines, string author)
    {
        try
        {
            // ניקוי גרשיים אם יש
            output = output?.Trim('"');

            // ולידציה לשדות
            if (string.IsNullOrWhiteSpace(language))
            {
                Console.WriteLine("❌ חובה להזין שפה אחת לפחות או 'all'.");
                return;
            }

            if (sort != "name" && sort != "type")
            {
                Console.WriteLine("❌ ערך לא חוקי עבור sort. אפשר רק 'name' או 'type'.");
                return;
            }

            // עיבוד רשימת שפות
            var languages = new List<string>();
            if (language.ToLower() != "all")
            {
                languages = language.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(l => l.Trim().ToLower())
                                    .Where(IsValidLanguage)
                                    .ToList();

                if (languages.Count == 0)
                {
                    Console.WriteLine("❌ לא הוזנה אף שפה חוקית.");
                    return;
                }
            }

            // הגדרת סיומות תקינות
            var validExtensions = new Dictionary<string, string>
            {
                { ".cs", "csharp" },
                { ".java", "java" },
                { ".py", "python" },
                { ".html", "html" }
            };

            // איתור כל הקבצים (כולל תיקיות משנה), בלי bin/obj/debug
            var allFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.*", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\") && !f.Contains("\\debug\\"))
                .Where(f => validExtensions.ContainsKey(Path.GetExtension(f).ToLower()));

            // סינון לפי שפה
            if (language.ToLower() != "all")
                allFiles = allFiles.Where(f => languages.Contains(validExtensions[Path.GetExtension(f).ToLower()]));

            // סידור לפי sort
            allFiles = sort == "type"
                ? allFiles.OrderBy(f => Path.GetExtension(f))
                : allFiles.OrderBy(f => Path.GetFileName(f));

            var files = allFiles.ToList();

            if (files.Count == 0)
            {
                Console.WriteLine("⚠️ לא נמצאו קבצים תואמים בתיקייה.");
                return;
            }

            // הגדרת שם קובץ ברירת מחדל
            string outputPath = string.IsNullOrEmpty(output)
                ? Path.Combine(Directory.GetCurrentDirectory(), "bundle.txt")
                : Path.GetFullPath(output);

            using (var writer = new StreamWriter(outputPath))
            {
                // כתיבת author
                if (!string.IsNullOrEmpty(author))
                    writer.WriteLine($"// נכתב על ידי: {author}");

                writer.WriteLine($"// נוצר בתאריך: {DateTime.Now}");
                writer.WriteLine();

                // מעבר על כל קובץ
                foreach (var file in files)
                {
                    if (note)
                        writer.WriteLine($"// מקור הקובץ: {Path.GetRelativePath(Directory.GetCurrentDirectory(), file)}");

                    var content = File.ReadAllText(file);

                    if (removeEmptyLines)
                        content = string.Join(Environment.NewLine,
                            content.Split(Environment.NewLine)
                                   .Where(line => !string.IsNullOrWhiteSpace(line)));

                    writer.WriteLine(content);
                    writer.WriteLine(); // רווח בין קבצים
                }
            }

            Console.WriteLine($"✅ הקובץ נוצר בהצלחה: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ שגיאה: {ex.Message}");
        }
    }

    static async Task CreateResponseFile()
    {
        Console.WriteLine("אנא הזן את שם השפה (לדוגמה: csharp,java או all):");
        string language = Console.ReadLine();

        Console.WriteLine("אנא הזן את שם קובץ ה-bundle (ללא רווחים או עטוף במרכאות):");
        string output = Console.ReadLine();

        Console.WriteLine("האם לרשום את מקור הקוד כהערה? (yes/no):");
        bool note = Console.ReadLine()?.ToLower() == "yes";

        Console.WriteLine("איך לסדר את הקבצים? (name/type):");
        string sort = Console.ReadLine();
        if (sort != "name" && sort != "type") sort = "name";

        Console.WriteLine("האם למחוק שורות ריקות? (yes/no):");
        bool removeEmptyLines = Console.ReadLine()?.ToLower() == "yes";

        Console.WriteLine("אנא הזן את שם היוצר:");
        string author = Console.ReadLine();

        // שם האפליקציה שלך (לא dotnet)
        string appName = AppDomain.CurrentDomain.FriendlyName.Replace(".dll", "").Replace(".exe", "");

        string command =
            $"{appName} bundle --language {language} --output {output} " +
            $"{(note ? "--note " : "")}" +
            $"--sort {sort} " +
            $"{(removeEmptyLines ? "--remove-empty-lines " : "")}" +
            $"{(!string.IsNullOrEmpty(author) ? $"--author \"{author}\"" : "")}";

        string responseFilePath = Path.Combine(Directory.GetCurrentDirectory(), "response.rsp");
        await File.WriteAllTextAsync(responseFilePath, command);

        Console.WriteLine($"✅ קובץ תגובה נוצר בהצלחה: {responseFilePath}");
    }

    static bool IsValidLanguage(string language)
    {
        var validLanguages = new[] { "csharp", "java", "python", "html" };
        return validLanguages.Contains(language.ToLower());
    }
}

