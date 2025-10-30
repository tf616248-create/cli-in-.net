// וסרצוג

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
        // הגדרת אפשרות חובה --language
        var languageOption = new Option<string>(
            "--language",
            "רשימת שפות תכנות, או 'all' לכל הקבצים")
        {
            IsRequired = true,
           
        };
      
        languageOption.AddAlias("-l");
        // הגדרת אפשרות לאופציה output לשם קובץ ה-bundle
        var outputOption = new Option<string>(
            "--output",
            "שם הקובץ המיוצא או נתיב מלא")
        {
            IsRequired = false,
          
        };
        outputOption.AddAlias("-o");
        // הגדרת אפשרות לרשום הערות מקור
        var noteOption = new Option<bool>(
            "--note",
            "לרשום את מקור הקוד כהערה בקובץ ה-bundle")
        {
            IsRequired = false,
        };
        noteOption.AddAlias("-n");
        // הגדרת אפשרות לסדר את הקבצים
        Option<string> sortOption = new Option<string>(
            "--sort",
            "סדר את קבצי הקוד לפי 'name' או 'type'")
        {
            IsRequired = false,
        };
        sortOption.SetDefaultValue("name");
        sortOption.AddAlias("-s");
        // הגדרת אפשרות למחוק שורות ריקות
        var removeEmptyLinesOption = new Option<bool>(
            "--remove-empty-lines",
            "למחוק שורות ריקות מקוד המקור")
        {
            IsRequired = false,
        };
        removeEmptyLinesOption.AddAlias("-r");
        // הגדרת אפשרות לרשום את שם היוצר
        var authorOption = new Option<string>(
            "--author",
            "שם יוצר הקובץ")
        {
            IsRequired = false,
        };
        authorOption.AddAlias("-a");
        // יצירת הפקודה bundle
        var bundleCommand = new Command("bundle", "אורז קבצי קוד לקובץ אחד")
        {
            languageOption,
            outputOption,
            noteOption,
            sortOption,
            removeEmptyLinesOption,
            authorOption
        };

        // שימוש ב-SetHandler במקום CommandHandler
        bundleCommand.SetHandler((string language, string output, bool note, string sort, bool removeEmptyLines, string author) =>
        {
            HandleBundle(language, output, note, sort, removeEmptyLines, author);
        }, languageOption, outputOption, noteOption, sortOption, removeEmptyLinesOption, authorOption);

        // יצירת הפקודה create-rsp
        var createRspCommand = new Command("create-rsp", "יוצר קובץ תגובה עם פקודה מוכנה");
        createRspCommand.SetHandler(async () =>
        {
            await CreateResponseFile();
        });

        // פקודת שורש
        var rootCommand = new RootCommand("כלי CLI לדוגמה");
        rootCommand.AddCommand(bundleCommand);
        rootCommand.AddCommand(createRspCommand);

        // הפעלה
        return await rootCommand.InvokeAsync(args);
    }

    static void HandleBundle(string language, string output, bool note, string sort, bool removeEmptyLines, string author)
    {
        // בדיקת תקינות השפה
        if (language != "all" && !IsValidLanguage(language))
        {
            Console.WriteLine("שפה לא תקינה. אנא הזן שפה תקינה או 'all'.");
            return;
        }

        // טיפול בקבצים
 
        // הגדרת סיומות קבצים תקינות
        var validExtensions = new List<string> { ".cs", ".java", ".py", ".html" }; // הוסף סיומות נוספות לפי הצורך

        // קבלת הקבצים על פי הסיומות
        var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.*", SearchOption.TopDirectoryOnly)
            .Where(file => validExtensions.Contains(Path.GetExtension(file).ToLower())); // כולל רק קבצים עם סיומות תקינות


        if (language != "all")
        {
            files = files.Where(file => IsFileOfLanguage(file, language)); // מסנן לפי שפה
        }

        // אם יש לסדר את הקבצים
        if (sort == "type")
        {
            files = files.OrderBy(file => GetFileType(file)); // סידור לפי סוג הקובץ
        }
        else
        {
            files = files.OrderBy(file => Path.GetFileName(file)); // סידור לפי שם הקובץ
        }

        // טיפול בשם הקובץ המיוצא
        string outputPath = string.IsNullOrEmpty(output) ? "defaultBundle.txt" : Path.GetFullPath(output);

        // בדיקה אם הנתיב תקין
        try
        {
            using (var writer = new StreamWriter(outputPath))
            {
                // אם יש לרשום הערות מקור
                if (note)
                {
                    writer.WriteLine($"// מקור הקוד: {language}");
                }

                // אם יש לרשום את שם היוצר
                if (!string.IsNullOrEmpty(author))
                {
                    writer.WriteLine($"// נכתב על ידי: {author}");
                }

                foreach (var file in files)
                {
                    var content = File.ReadAllText(file);
                    if (removeEmptyLines)
                    {
                        content = string.Join(Environment.NewLine, content.Split(Environment.NewLine)
                            .Where(line => !string.IsNullOrWhiteSpace(line))); // מחיקת שורות ריקות
                    }
                    writer.WriteLine(content);
                }

                Console.WriteLine($"הקובץ נשמר ב-{outputPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"שגיאה בשמירה לקובץ: {ex.Message}. אנא ודא שהנתיב שסיפקת תקין ושהתיקייה קיימת.");
        }
    }

    static async Task CreateResponseFile()
    {
        Console.WriteLine("אנא הזן את שם השפה:");
        string language = Console.ReadLine();

        Console.WriteLine("אנא הזן את שם קובץ ה-bundle:");
        string output = Console.ReadLine();

        Console.WriteLine("האם לרשום את מקור הקוד כהערה? (yes/no):");
        bool note = Console.ReadLine()?.ToLower() == "yes";

        Console.WriteLine("איך לסדר את הקבצים? (name/type):");
        string sort = Console.ReadLine();

        Console.WriteLine("האם למחוק שורות ריקות? (yes/no):");
        bool removeEmptyLines = Console.ReadLine()?.ToLower() == "yes";

        Console.WriteLine("אנא הזן את שם היוצר:");
        string author = Console.ReadLine();

        // יצירת הפקודה המלאה
        string command = $"dotnet"+$" "+$"bundle --language {language} --output {output} " +
                         $"{(note ? "--note " : "")}" +
                         $"--sort {sort} " +
                         $"{(removeEmptyLines ? "--remove-empty-lines " : "")}" +
                         $"{(!string.IsNullOrEmpty(author) ? $"--author {author}" : "")}";

        // שמירה לקובץ תגובה
        string currentDirectory = Directory.GetCurrentDirectory();
        string responseFilePath = Path.Combine(currentDirectory, "response.rsp");

        await File.WriteAllTextAsync(responseFilePath, command);

        Console.WriteLine("קובץ התגובה נוצר בהצלחה: response.rsp");
    }

    static bool IsValidLanguage(string language)
    {
        var validLanguages = new List<string> { "csharp", "java", "python", "html" }; // הוספת html לרשימה
        return validLanguages.Contains(language.ToLower());
    }


    static string GetFileType(string filePath)
    {
        return Path.GetExtension(filePath).ToLower(); // מחזיר את סוג הקובץ
    }

    static bool IsFileOfLanguage(string filePath, string language)
    {
        // כאן תוכל להוסיף לוגיקה לבדוק אם הקובץ הוא מהשפה שהוזנה
        return Path.GetExtension(filePath).ToLower() switch
        {
            ".cs" when language == "csharp" => true,
            ".java" when language == "java" => true,
            ".py" when language == "python" => true,
            ".html" when language == "html" => true, 

            _ => false
        };
    }
}
