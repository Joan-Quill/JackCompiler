using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static JackCompiler.ConsoleWriter;

namespace JackCompiler
{
    internal class JackTokenizer
    {
        public enum TokenType
        {
            None,
            KEYWORD,
            SYMBOL,
            INT_CONST,
            STRING_CONST,
            IDENTIFIER
        }

        public static readonly string[] KeyWords = new string[]
        {
            "class", "constructor", "function", "method", "field", "static",
            "var", "int", "char", "boolean", "void", "true", "false", "null",
            "this", "do", "if", "else", "while", "return", "let"
        };

        public static readonly string[] Libraries = new string[]
        {
            "Array", "Math", "String", "Output", "Screen", "Keyboard", "Memory", "Sys"
        };

        public static readonly string Operations = "+-*/&|<>=";
        public static readonly string Symbols    = "{}()[].,;+-*/&|<>=~";
        public TokenType CurrentTokenType { get; private set; }

        private StreamReader? _stream;
        private List<string>? _tokens;
        private string _filePath = "";
        private int _pointer = 0;
        private string _currentToken;
        private bool _isFirst;


        public List<string> Tokens 
        { 
            get 
            {
                if (_tokens != null) { return _tokens; }
                else { ConsoleWrite(new string[] { "Tokens list was null. Call GetTokens() before using." }, ConsoleCode.ERROR); throw new Exception(); }
            } 
        }
        public string FilePath
        {
            get { return _filePath; }
            set
            {
                if (!string.IsNullOrEmpty(value)) _filePath = value;
                else { ConsoleWrite(new string[] { "File Path was not set: null or empty." }, ConsoleCode.ERROR); throw new Exception(); }
            }
        }
        public string CurrentToken
        {
            get { return _currentToken; }
            private set
            {
                if (!string.IsNullOrEmpty(value)) _currentToken = value;
                else { ConsoleWrite(new string[] { "Current token was not set: null or empty." }, ConsoleCode.ERROR); throw new Exception(); }
            }
        }

        public void GetTokens(string filePath)
        {
            _tokens = new List<string>();
            string? operatingLine = "";

            try { _stream = new StreamReader(filePath); }
            catch { ConsoleWrite(new string[] { "Could not generate input stream." }, ConsoleCode.ERROR); throw; }

            while (!_stream.EndOfStream)
            {
                operatingLine += _stream.ReadLine();
                if (operatingLine == null) { ConsoleWrite(new string[] { "Did not expect end of stream." }, ConsoleCode.ERROR); throw new Exception(); }
                else
                {
                    while (true)
                    {
                        if (HasComments(operatingLine)) { operatingLine = RemoveComments(operatingLine).Trim(); }
                        if (string.IsNullOrEmpty(operatingLine) && !_stream.EndOfStream) { operatingLine = _stream.ReadLine(); }
                        else break;
                    }
                }
            }

            
                    
            while (operatingLine.Length > 0)
            {
                int startLength = operatingLine.Length;

                while (operatingLine.StartsWith(' '))
                {
                    operatingLine = operatingLine[1..];
                }

                for (int i = 0; i < KeyWords.Length; i++)
                {
                    if (operatingLine.StartsWith(KeyWords[i]))
                    {
                        string keyword = KeyWords[i];
                        _tokens.Add(keyword);
                        operatingLine = operatingLine[keyword.Length..];
                    }
                }

                if (operatingLine.Length == 0) break;

                if (Symbols.Contains(operatingLine[0]))
                {
                    char symbol = operatingLine[0];
                    _tokens.Add(symbol.ToString());
                    operatingLine = operatingLine[1..];
                }
                else if (char.IsDigit(operatingLine[0]))
                {
                    string num = "";

                    while (char.IsDigit(operatingLine[0]))
                    {
                        num += operatingLine[0];
                        operatingLine = operatingLine[1..];
                    }
                    
                    if (Int16.TryParse(num, out short number))
                    {
                        _tokens.Add(number.ToString());
                    }
                    else
                    {
                        ConsoleWrite(new string[] { "Could not parse integer." }, ConsoleCode.ERROR); throw new Exception();
                    }
                }
                else if (operatingLine.StartsWith("\""))
                {
                    string opString = "";
                    operatingLine = operatingLine[1..];
                    while(!operatingLine.StartsWith("\""))
                    {
                        if (string.IsNullOrEmpty(operatingLine)) 
                        { 
                            ConsoleWrite(new string[] { "Could not find end of string." }, ConsoleCode.ERROR); throw new Exception(); 
                        }
                        opString += operatingLine[0];
                        operatingLine = operatingLine[1..];
                    }
                    opString = $"\"{opString}\"";
                    _tokens.Add(opString);
                    operatingLine = operatingLine[1..];
                }
                else if (char.IsLetter(operatingLine[0]) || operatingLine.StartsWith('_'))
                {
                    string opIdentifier = operatingLine[0..1];
                    operatingLine = operatingLine[1..];
                    while (char.IsLetter(operatingLine[0]) || operatingLine.StartsWith('_'))
                    {
                        opIdentifier += operatingLine[0..1];
                        operatingLine = operatingLine[1..];
                    }

                    _tokens.Add(opIdentifier);
                }

                if (!(operatingLine.Length < startLength))
                {
                    operatingLine = operatingLine[1..];
                }

            }

            _isFirst = true;
            _pointer = 0;
        }

        public bool HasMoreTokens()
        {
            bool hasMoreTokens = false;
            if (_pointer < Tokens.Count - 1)
            {
                hasMoreTokens = true;
            }
            return hasMoreTokens;
        }

        public void Advance()
        {
            if (HasMoreTokens())
            {
                if (!_isFirst) { _pointer++; }
                else if (_isFirst) { _isFirst = false; }

                string opLine = Tokens[_pointer];
                
                if (KeyWords.Contains(opLine))
                {
                    CurrentTokenType = TokenType.KEYWORD;
                    CurrentToken = opLine;
                }
                else if (Symbols.Contains(opLine))
                {
                    CurrentTokenType = TokenType.SYMBOL;
                    CurrentToken = opLine;
                }
                else if (Int16.TryParse(opLine, out short _))
                {
                    CurrentTokenType = TokenType.INT_CONST;
                    CurrentToken = opLine;
                }
                else if (opLine.StartsWith("\"") && opLine.EndsWith("\""))
                {
                    CurrentTokenType = TokenType.STRING_CONST;
                    CurrentToken = opLine[1..^1];
                }
                else if (char.IsLetter(opLine[0]) || opLine.StartsWith('_'))
                {
                    CurrentTokenType = TokenType.IDENTIFIER;
                    CurrentToken = opLine;
                }
                else
                {
                    ConsoleWrite(new string[] { "While advancing, could not parse token." }, ConsoleCode.ERROR); throw new Exception();
                }
            }
            else
            {
                ConsoleWrite(new string[] { "Advance was called, but there are no more tokens." }, ConsoleCode.ERROR);
            }
        }

        public string PeekToken()
        {
            return Tokens[_pointer + 1];
        }

        public bool IsOperation()
        {
            if (char.TryParse(CurrentToken, out char operation))
            {
                if (Operations.Contains(operation)) return true;
            }
            return false;
        }

        private bool HasComments(string opLine)
        {
            bool hasComments = false;
            if (opLine.Contains("//") || opLine.Contains("/*") || opLine.StartsWith(" *"))
            {
                hasComments = true;
            }
            return hasComments;
        }

        private string RemoveComments(string opLine)
        {
            string noComments = opLine;
            if (HasComments(opLine))
            {
                int offSet;
                if (opLine.StartsWith(" *"))
                {
                    offSet = opLine.IndexOf("*");
                }
                else if (opLine.Contains("/*"))
                {
                    offSet = opLine.IndexOf("/*");
                }
                else
                {
                    offSet = opLine.IndexOf("//");
                }
                noComments = opLine.Substring(0, offSet).Trim();

            }
            return noComments;
        }


    }
}
