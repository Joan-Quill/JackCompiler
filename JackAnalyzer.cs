using static JackCompiler.ConsoleWriter;

namespace JackCompiler
{
    internal class JackAnalyzer
    {
        private static List<string> FilePaths = new();

        static void Main(string[] args)
        {
            string path;

            while (true)
            {
                if (args.Length > 0)
                {
                    // If a command-line argument is provided, use it as the path
                    path = args[0];
                    if (!IsValidPath(path))
                    {
                        ConsoleWrite(new string[] { "The specified path does not exist." }, ConsoleCode.ERROR);
                        return;
                    }
                    path = Path.GetFullPath(path);
                }
                else
                {
                    // If no command-line argument is provided, ask the user for the path
                    ConsoleWrite(new string[] { "Enter a valid file or directory path." }, ConsoleCode.MESSAGE);
                    Console.Write(">");
                    path = Console.ReadLine();
                    if (!IsValidPath(path))
                    {
                        ConsoleWrite(new string[] { "The specified path does not exist." }, ConsoleCode.ERROR);
                        continue;
                    }
                    path = Path.GetFullPath(path);
                }

                break;
            }

            if (Directory.Exists(path))
            {
                // If the path is a directory, search for .jack files inside the directory
                FilePaths = Directory.GetFiles(path, "*.jack", SearchOption.TopDirectoryOnly).ToList();

                foreach (string file in FilePaths)
                {
                    ConsoleWrite(new string[] { $"File capture at {file}" }, ConsoleCode.MESSAGE);
                }
            }
            else if (File.Exists(path))
            {
                FilePaths.Add(path);
                ConsoleWrite(new string[] { $"File capture at {FilePaths[0]}" }, ConsoleCode.MESSAGE);
            }

            //JackTokenizer tokenizer = new();

            //foreach (string file in FilePaths)
            //{
            //    ConsoleWrite(new string[] { file }, ConsoleCode.MESSAGE, ConsoleOptions.ConsoleBar);
            //    tokenizer.GetTokens(file);
            //    for (int i = 0; i < tokenizer.Tokens.Count; i++)
            //    {
            //        tokenizer.Advance();
            //        Console.WriteLine($"{tokenizer.CurrentTokenType} : {tokenizer.CurrentToken}");
            //    }
            //}

            CompilationEngine ce;
            
            foreach (string file in FilePaths)
            {
                ConsoleWrite(new string[] { $"Working on {Path.GetFileName(file)}" }, ConsoleCode.MESSAGE, ConsoleOptions.ConsoleBar);
                ce = new();
                ce.filePath = file;
                ce.Compile();
                ConsoleWrite(new string[] { $"Compiled {Path.GetFileName(file)} to XML" }, ConsoleCode.SUCCESS);
            }


        }

        static bool IsJackFile(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            return extension == ".jack";
        }

        static bool IsValidPath(string path)
        {
            char[] invalidChars = Path.GetInvalidPathChars();
            bool pathExists = Directory.Exists(path) || File.Exists(path);

            return !string.IsNullOrEmpty(path) && !path.Any(c => invalidChars.Contains(c)) && pathExists && (IsJackFile(path) || HasJackFiles(path));
        }

        static bool HasJackFiles(string path)
        {
            return Directory.Exists(path) && Directory.GetFiles(path, "*.jack", SearchOption.TopDirectoryOnly).Length > 0;
        }

    }
}