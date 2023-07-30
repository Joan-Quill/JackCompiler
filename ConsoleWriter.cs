namespace JackCompiler
{
    internal class ConsoleWriter
    {
        public enum ConsoleCode
        {
            None,
            MESSAGE,
            ERROR,
            FINISH,
            DEBUG,
            SUCCESS
        }

        [Flags] public enum ConsoleOptions
        {
            None       = 0b_0000,
            Wait       = 0b_0001,
            ConsoleBar = 0b_0010
        }

        public static void ConsoleBar()
        {
            Console.WriteLine(new string('─', Console.BufferWidth - 1));
        }

        //Writes a specified set of lines to the console prefixed with a specified code
        public static void ConsoleWrite(string[] msg, ConsoleCode code = ConsoleCode.None, ConsoleOptions options = ConsoleOptions.None)
        {
            if (options.HasFlag(ConsoleOptions.ConsoleBar)) 
            { 
                ConsoleBar(); 
            }

            ConsoleColor defaultColor = Console.ForegroundColor;
            int space = -3;

            switch (code)
            {
                case ConsoleCode.None:
                    break;
                
                case ConsoleCode.MESSAGE:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"[{nameof(ConsoleCode.MESSAGE)}] ");
                    Console.ForegroundColor = defaultColor;
                    space = nameof(ConsoleCode.MESSAGE).Length;
                    break;

                case ConsoleCode.ERROR:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"[{nameof(ConsoleCode.ERROR)}] ");
                    Console.ForegroundColor = defaultColor;
                    space = nameof(ConsoleCode.ERROR).Length;
                    break;

                case ConsoleCode.FINISH:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write($"[{nameof(ConsoleCode.FINISH)}] ");
                    Console.ForegroundColor = defaultColor;
                    space = nameof(ConsoleCode.FINISH).Length;
                    break;

                case ConsoleCode.DEBUG:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.Write($"[{nameof(ConsoleCode.DEBUG)}] ");
                    Console.ForegroundColor = defaultColor;
                    space = nameof(ConsoleCode.DEBUG).Length;
                    break;

                case ConsoleCode.SUCCESS:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"[{nameof(ConsoleCode.SUCCESS)}] ");
                    Console.ForegroundColor = defaultColor;
                    space = nameof(ConsoleCode.SUCCESS).Length;
                    break;
            }
            
            ConWR(msg, options.HasFlag(ConsoleOptions.Wait), space);

        }

        private static void ConWR(string[] msg, bool wait, int space)
        {
            string spacer = new string(' ', space + 3);
            
            for (int i = 0; i < msg.Length; i++)
            {
                if (i < msg.Length - 1 || !wait)
                {
                    if (i == 0) Console.WriteLine(msg[i]);
                    else Console.WriteLine(spacer + msg[i]);
                }
                else
                {
                    Console.Write(spacer + msg[i]);
                    Console.ReadKey();
                    Console.WriteLine();
                }
            }
        }

    }
}
