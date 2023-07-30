using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static JackCompiler.ConsoleWriter;

namespace JackCompiler
{
    internal class CompilationEngine
    {
        private static readonly string[] Subroutines = new string[]
        {
            "function", "method", "constructor"
        };

        private JackTokenizer tokenizer = new();
        private XmlWriter? xmlWriter;

        private bool IsFirstSubroutine = true;
        private string _filePath = "";
        public string filePath { 
            get { return _filePath; } 
            set { 
                if (!string.IsNullOrEmpty(value)) _filePath = value;
                else { ConsoleWrite(new string[] { "File Path was not set: null or empty." }, ConsoleCode.ERROR); throw new Exception(); }
            } 
        }

        public void Compile()
        {
            try
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.NewLineOnAttributes = true;
                settings.IndentChars = "\t";
                xmlWriter = XmlWriter.Create(filePath.Replace(".jack", ".xml"), settings);
                tokenizer.GetTokens(filePath);

                
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("class");

                tokenizer.Advance();
                ElementWriter(tokenizer, xmlWriter); //writes first element <keyword>class</keyword>

                tokenizer.Advance();
                ElementWriter(tokenizer, xmlWriter); //writes class identifier

                tokenizer.Advance();
                ElementWriter(tokenizer, xmlWriter); //writes opening curly bracket

                tokenizer.Advance();
                if (tokenizer.CurrentToken.Equals("static") || tokenizer.CurrentToken.Equals("field"))
                {
                    CompileClassVarDec(tokenizer, xmlWriter);
                }
                
                tokenizer.Advance();
                CompileSubroutine(tokenizer, xmlWriter);
                
                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndDocument();
                xmlWriter.Close();

            }
            catch
            {
                ConsoleWrite(new string[] { "Compile failure." }, ConsoleCode.ERROR); xmlWriter.Close(); throw;
            }
        }

        private void ElementWriter(JackTokenizer jt, XmlWriter xml)
        {
            switch (jt.CurrentTokenType)
            {
                case JackTokenizer.TokenType.KEYWORD:
                    xml.WriteStartElement("keyword");
                    xml.WriteString(jt.CurrentToken);
                    xml.WriteEndElement();
                    break;

                case JackTokenizer.TokenType.SYMBOL:
                    xml.WriteStartElement("symbol");
                    xml.WriteString(jt.CurrentToken);
                    xml.WriteEndElement();
                    break;

                case JackTokenizer.TokenType.INT_CONST:
                    xml.WriteStartElement("integerConstant");
                    xml.WriteString(jt.CurrentToken);
                    xml.WriteEndElement();
                    break;

                case JackTokenizer.TokenType.STRING_CONST:
                    xml.WriteStartElement("stringConstant");
                    xml.WriteString(jt.CurrentToken);
                    xml.WriteEndElement();
                    break;

                case JackTokenizer.TokenType.IDENTIFIER:
                    xml.WriteStartElement("identifier");
                    xml.WriteString(jt.CurrentToken);
                    xml.WriteEndElement();
                    break;
            }
        }

        private void CompileClassVarDec(JackTokenizer jt, XmlWriter xml)
        {
            try
            {
                string token = jt.CurrentToken;
                if (!token.Equals("static") && !token.Equals("field"))
                {
                    ConsoleWrite(new string[] { $"Expected static or field variable declaration. Got: {token}" }, ConsoleCode.ERROR); throw new Exception();
                }
                while (token.Equals("static") || token.Equals("field"))
                {
                    xml.WriteStartElement("classVarDec");
                    ElementWriter(jt, xml);

                    jt.Advance();
                    if (jt.CurrentTokenType != JackTokenizer.TokenType.KEYWORD && jt.CurrentTokenType != JackTokenizer.TokenType.IDENTIFIER)
                    {
                        ConsoleWrite(new string[] { $"Expected keyword or identifier. Got: {token}" }, ConsoleCode.ERROR); throw new Exception();
                    }
                    ElementWriter(jt, xml);

                    jt.Advance();
                    if (jt.CurrentTokenType != JackTokenizer.TokenType.IDENTIFIER)
                    {
                        ConsoleWrite(new string[] { $"Expected identifier. Got: {token}" }, ConsoleCode.ERROR); throw new Exception();
                    }
                    ElementWriter(jt, xml);

                    jt.Advance();
                    token = jt.CurrentToken;
                    while (token.Equals(","))
                    {
                        ElementWriter(jt, xml);
                        jt.Advance();
                        token = jt.CurrentToken;
                        if (jt.CurrentTokenType != JackTokenizer.TokenType.IDENTIFIER)
                        {
                            ConsoleWrite(new string[] { $"Expected identifier. Got: {token}" }, ConsoleCode.ERROR); throw new Exception();
                        }
                        ElementWriter(jt, xml);
                        jt.Advance();
                        token = jt.CurrentToken;
                    }

                    if (!token.Equals(";"))
                    {
                        ConsoleWrite(new string[] { $"Expected end of line. Got: {token}" }, ConsoleCode.ERROR); throw new Exception();
                    }
                    ElementWriter(jt, xml);

                    xml.WriteEndElement();

                    if (Subroutines.Contains(jt.PeekToken())) return;
                    else { jt.Advance(); token = jt.CurrentToken; }
                
                }
            }
            catch
            {
                ConsoleWrite(new string[] { "Failed in CompileClassVarDec()" }, ConsoleCode.ERROR); throw;
            }

        }

        private void CompileSubroutine(JackTokenizer jt, XmlWriter xml)
        {
            try
            {
                bool hasSubroutines = false;
                string token = jt.CurrentToken;

                if (token.Equals("}"))
                {
                    ElementWriter(jt, xml);
                    return;
                }

                if (IsFirstSubroutine && Subroutines.Contains(token))
                {
                    IsFirstSubroutine = false;
                    xml.WriteStartElement("subroutineDec");
                    hasSubroutines = true;
                }

                if (Subroutines.Contains(token))
                {
                    hasSubroutines = true;
                    ElementWriter(jt, xml);
                    jt.Advance();
                    token = jt.CurrentToken;
                }

                if (jt.CurrentTokenType == JackTokenizer.TokenType.IDENTIFIER || jt.CurrentTokenType == JackTokenizer.TokenType.KEYWORD)
                {
                    ElementWriter(jt, xml);
                    jt.Advance();
                    token = jt.CurrentToken;
                }

                if (jt.CurrentTokenType == JackTokenizer.TokenType.IDENTIFIER)
                {
                    ElementWriter(jt, xml);
                    jt.Advance();
                    token = jt.CurrentToken;
                }

                if (token.Equals("("))
                {
                    ElementWriter(jt, xml);
                    xml.WriteStartElement("parameterList");

                    jt.Advance();
                    CompileParameterList(jt, xml);
                    
                    xml.WriteEndElement();
                    ElementWriter(jt, xml);
                    jt.Advance();
                    token = jt.CurrentToken;
                }

                if (token.Equals("{"))
                {
                    xml.WriteStartElement("subroutineBody");
                    ElementWriter(jt, xml);
                    jt.Advance();
                    token = jt.CurrentToken;
                }

                while (token.Equals("var"))
                {
                    xml.WriteStartElement("varDec");
                    CompileVarDec(jt, xml);
                    xml.WriteEndElement();
                    token = jt.CurrentToken;
                }

                xml.WriteStartElement("statements");
                CompileStatements(jt, xml);
                xml.WriteEndElement();

                ElementWriter(jt, xml);
                if (hasSubroutines)
                {
                    xml.WriteEndElement();
                    xml.WriteEndElement();
                    IsFirstSubroutine = true;
                }

                jt.Advance();
                CompileSubroutine(jt, xml);

            
            }
            catch
            {
                ConsoleWrite(new string[] { "Failed in CompileSubroutine()" }, ConsoleCode.ERROR); throw;
            }
        }

        private void CompileParameterList(JackTokenizer jt, XmlWriter xml)
        {
            try
            {
                string token = jt.CurrentToken;
                while (!(jt.CurrentTokenType == JackTokenizer.TokenType.SYMBOL && token.Equals(")")))
                {
                    if (jt.CurrentTokenType == JackTokenizer.TokenType.IDENTIFIER)
                    {
                        ElementWriter(jt, xml);
                        jt.Advance();
                        token = jt.CurrentToken;
                    }
                    else if (jt.CurrentTokenType == JackTokenizer.TokenType.KEYWORD)
                    {
                        ElementWriter(jt, xml);
                        jt.Advance();
                        token = jt.CurrentToken;
                    }
                    else if (token.Equals(","))
                    {
                        ElementWriter(jt, xml);
                        jt.Advance();
                        token = jt.CurrentToken;
                    }
                    else
                    {
                        ConsoleWrite(new string[] { $"Improper parameter list. Got: {token}" }, ConsoleCode.ERROR); throw new Exception();
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        private void CompileVarDec(JackTokenizer jt, XmlWriter xml)
        {
            try
            {
                string token = jt.CurrentToken;
                if (token.Equals("var"))
                {
                    ElementWriter(jt, xml);
                    jt.Advance();
                    token = jt.CurrentToken;
                }

                if (jt.CurrentTokenType == JackTokenizer.TokenType.IDENTIFIER || jt.CurrentTokenType == JackTokenizer.TokenType.KEYWORD)
                {
                    ElementWriter(jt, xml);
                    jt.Advance();
                    token = jt.CurrentToken;
                }

                if (jt.CurrentTokenType == JackTokenizer.TokenType.IDENTIFIER)
                {
                    ElementWriter(jt, xml);
                    jt.Advance();
                    token = jt.CurrentToken;
                }

                while (token.Equals(","))
                {
                    ElementWriter(jt, xml);
                    jt.Advance();
                    ElementWriter(jt, xml);
                    jt.Advance();
                    token = jt.CurrentToken;
                }

                if (token.Equals(";"))
                {
                    ElementWriter(jt, xml);
                    jt.Advance();
                    token = jt.CurrentToken;
                }

            }
            catch
            {
                ConsoleWrite(new string[] { "Failed in CompileVarDec()" }, ConsoleCode.ERROR); throw;
            }
        }

        private void CompileStatements(JackTokenizer jt, XmlWriter xml)
        {
            try
            {
                string token = jt.CurrentToken;
                if (token.Equals("}"))
                {
                    return;
                }
                else if (token.Equals("do"))
                {
                    xml.WriteStartElement("doStatement");
                    CompileDo(jt, xml);
                    xml.WriteEndElement();
                }
                else if (token.Equals("let"))
                {
                    xml.WriteStartElement("letStatement");
                    CompileLet(jt, xml);
                    xml.WriteEndElement();
                }
                else if (token.Equals("if"))
                {
                    xml.WriteStartElement("ifStatement");
                    CompileIf(jt, xml);
                    xml.WriteEndElement();
                }
                else if (token.Equals("while"))
                {
                    xml.WriteStartElement("whileStatement");
                    CompileWhile(jt, xml);
                    xml.WriteEndElement();
                }
                else if (token.Equals("return"))
                {
                    xml.WriteStartElement("returnStatement");
                    CompileReturn(jt, xml);
                    xml.WriteEndElement();
                }
                else { ConsoleWrite(new string[] { $"Expected statement type. Got: {token}" }, ConsoleCode.ERROR); throw new Exception(); }
                
                jt.Advance();
                CompileStatements(jt, xml);
            }
            catch
            {
                ConsoleWrite(new string[] { "Failed in CompileStatements()" }, ConsoleCode.ERROR); throw;
            }
        }

        private void CompileDo(JackTokenizer jt, XmlWriter xml)
        {
            try
            {
                ElementWriter(jt, xml);

                jt.Advance();
                CompileCall(jt, xml);
            }
            catch
            {
                ConsoleWrite(new string[] { "Failed in CompileDo()" }, ConsoleCode.ERROR); throw;
            }
        }

        private void CompileCall(JackTokenizer jt, XmlWriter xml)
        {
            try
            {
                string token;
                token = jt.CurrentToken;
                if (jt.CurrentTokenType != JackTokenizer.TokenType.IDENTIFIER) { ConsoleWrite(new string[] { $"Expected call identifier. Got: {token}" }, ConsoleCode.ERROR); throw new Exception(); }
                ElementWriter(jt, xml);

                jt.Advance();
                token = jt.CurrentToken;

                if (token.Equals("."))
                {
                    ElementWriter(jt, xml);

                    jt.Advance();
                    token = jt.CurrentToken;
                    if (jt.CurrentTokenType != JackTokenizer.TokenType.IDENTIFIER) { ConsoleWrite(new string[] { $"Expected function call identifier. Got: {token}" }, ConsoleCode.ERROR); throw new Exception(); }
                    ElementWriter(jt, xml);

                    jt.Advance();
                    token = jt.CurrentToken;
                    if (!token.Equals("(")) { ConsoleWrite(new string[] { $"Expected opening parentheses. Got: {token}" }, ConsoleCode.ERROR); throw new Exception(); }
                    ElementWriter(jt, xml);

                    jt.Advance();
                    xml.WriteStartElement("expressionList");
                    CompileExpressionList(jt, xml);
                    xml.WriteEndElement();

                    if (!jt.CurrentToken.Equals(")")) jt.Advance();
                    token = jt.CurrentToken;
                    if (!token.Equals(")")) { ConsoleWrite(new string[] { $"Expected ending parentheses. Got: {token}" }, ConsoleCode.ERROR); throw new Exception(); }
                    ElementWriter(jt, xml);

                    jt.Advance();
                    if (!jt.CurrentToken.Equals(";")) { ConsoleWrite(new string[] { $"Expected end of line. Got: {token}" }, ConsoleCode.ERROR); throw new Exception(); }
                    ElementWriter(jt, xml);
                    return;
                }
                else if (token.Equals("("))
                {
                    ElementWriter(jt, xml);

                    jt.Advance();
                    xml.WriteStartElement("expressionList");
                    CompileExpressionList(jt, xml);
                    xml.WriteEndElement();

                    if (!jt.CurrentToken.Equals(")")) jt.Advance();
                    token = jt.CurrentToken;
                    if (!token.Equals(")")) { ConsoleWrite(new string[] { $"Expected closing parentheses. Got: {token}" }, ConsoleCode.ERROR); throw new Exception(); }
                    ElementWriter(jt, xml);

                    jt.Advance();
                    if (!jt.CurrentToken.Equals(";")) { ConsoleWrite(new string[] { $"Expected end of line. Got: {token}" }, ConsoleCode.ERROR); throw new Exception(); }
                    ElementWriter(jt, xml);
                    return;
                }
                else
                {
                    ConsoleWrite(new string[] { $"Improper function format: missing parentheses or period. Got: {token}" }, ConsoleCode.ERROR); throw new Exception();
                }

            }
            catch
            {
                ConsoleWrite(new string[] { "Failed in CompileDo()" }, ConsoleCode.ERROR); throw;
            }
        }

        private void CompileLet(JackTokenizer jt, XmlWriter xml)
        {
            try
            {
                string token;

                ElementWriter(jt, xml);

                jt.Advance();
                token = jt.CurrentToken;
                if (jt.CurrentTokenType != JackTokenizer.TokenType.IDENTIFIER) { ConsoleWrite(new string[] { $"Expected identifier. Got: {token}" }, ConsoleCode.ERROR); throw new Exception(); }
                ElementWriter(jt, xml);

                jt.Advance();
                token = jt.CurrentToken;

                if (token.Equals("["))
                {
                    ElementWriter(jt, xml);

                    jt.Advance();
                    CompileExpression(jt, xml);

                    jt.Advance();
                    token = jt.CurrentToken;
                    if (token.Equals("]"))
                    {
                        ElementWriter(jt, xml);
                    }

                    jt.Advance();
                    token = jt.CurrentToken;
                }

                if (!token.Equals("=")) { ConsoleWrite(new string[] { $"Expected '=' symbol. Got: {token}" }, ConsoleCode.ERROR); throw new Exception(); }
                ElementWriter(jt, xml);

                jt.Advance();
                CompileExpression(jt, xml);
                token = jt.CurrentToken;

                if (!token.Equals(";")) jt.Advance();
                token = jt.CurrentToken;
                if (!token.Equals(";")) { ConsoleWrite(new string[] { $"Expected end of line. Got: {token}" }, ConsoleCode.ERROR); throw new Exception(); }
                ElementWriter(jt, xml);

                //jt.Advance();
            }
            catch
            {
                ConsoleWrite(new string[] { "Failed in CompileLet()" }, ConsoleCode.ERROR); throw;
            }
        }

        private void CompileWhile(JackTokenizer jt, XmlWriter xml)
        {
            try
            {
                string token;
                
                ElementWriter(jt, xml);

                jt.Advance();
                token = jt.CurrentToken;
                if (!token.Equals("(")) { ConsoleWrite(new string[] { $"Expected opening parentheses. Got: {token}" }, ConsoleCode.ERROR); throw new Exception(); }
                ElementWriter(jt, xml);

                jt.Advance();
                CompileExpression(jt, xml);

                jt.Advance();
                token = jt.CurrentToken;
                if (!token.Equals(")")) { ConsoleWrite(new string[] { $"Expected closing parentheses. Got: {token}" }, ConsoleCode.ERROR); throw new Exception(); }
                ElementWriter(jt, xml);

                jt.Advance();
                token = jt.CurrentToken;
                if (!token.Equals("{")) { ConsoleWrite(new string[] { $"Expected opening curly bracket. Got: {token}" }, ConsoleCode.ERROR); throw new Exception(); }
                ElementWriter(jt, xml);

                xml.WriteStartElement("statements");
                jt.Advance();
                CompileStatements(jt, xml);
                xml.WriteEndElement();

                //jt.Advance();
                token = jt.CurrentToken;
                if (!token.Equals("}")) { ConsoleWrite(new string[] { $"Expected closing curly bracket. Got: {token}" }, ConsoleCode.ERROR); throw new Exception(); }
                ElementWriter(jt, xml);
            }
            catch
            {
                ConsoleWrite(new string[] { "Failed in CompileWhile()" }, ConsoleCode.ERROR); throw;
            }
        }

        private void CompileReturn(JackTokenizer jt, XmlWriter xml)
        {
            try
            {
                ElementWriter(jt, xml);

                if (!jt.PeekToken().Equals(";"))
                {
                    jt.Advance();
                    CompileExpression(jt, xml);
                }

                jt.Advance();
                if (jt.CurrentToken.Equals(";"))
                {
                    ElementWriter(jt, xml);
                }
                else { ConsoleWrite(new string[] { $"Expected end of line. Got: {jt.CurrentToken}" }, ConsoleCode.ERROR); xml.Close(); throw new Exception(); }

            }
            catch
            {
                ConsoleWrite(new string[] { "Failed in CompileReturn()" }, ConsoleCode.ERROR); throw;
            }
        }

        private void CompileIf(JackTokenizer jt, XmlWriter xml)
        {
            try
            {
                ElementWriter(jt, xml);

                jt.Advance();
                string token = jt.CurrentToken;
                if (!token.Equals("(")) { ConsoleWrite(new string[] { $"Expected opening parentheses. Got: {token}" }, ConsoleCode.ERROR); throw new Exception(); }
                ElementWriter(jt, xml);

                jt.Advance();
                CompileExpression(jt, xml);

                jt.Advance();
                token = jt.CurrentToken;
                if (!token.Equals(")")) { ConsoleWrite(new string[] { $"Expected closing parentheses. Got: {token}" }, ConsoleCode.ERROR); throw new Exception(); }
                ElementWriter(jt, xml);

                jt.Advance();
                token = jt.CurrentToken;
                if (!token.Equals("{")) { ConsoleWrite(new string[] { $"Expected opening curly bracket. Got: {token}" }, ConsoleCode.ERROR); throw new Exception(); }
                ElementWriter(jt, xml);

                jt.Advance();
                xml.WriteStartElement("statements");
                CompileStatements(jt, xml);
                xml.WriteEndElement();

                //jt.Advance();
                token = jt.CurrentToken;
                if (!token.Equals("}")) { ConsoleWrite(new string[] { $"Expected closing curly bracket. Got: {token}" }, ConsoleCode.ERROR); throw new Exception(); }
                ElementWriter(jt, xml);

                if (jt.PeekToken().Equals("else"))
                {
                    jt.Advance();
                    ElementWriter(jt, xml);

                    jt.Advance();
                    token = jt.CurrentToken;
                    if (!token.Equals("{")) { ConsoleWrite(new string[] { $"Expected opening curly bracket. Got: {token}" }, ConsoleCode.ERROR); throw new Exception(); }
                    ElementWriter(jt, xml);

                    jt.Advance();
                    xml.WriteStartElement("statements");
                    CompileStatements(jt, xml);
                    xml.WriteEndElement();

                    //jt.Advance();
                    token = jt.CurrentToken;
                    if (!token.Equals("}")) { ConsoleWrite(new string[] { $"Expected closing curly bracket. Got: {token}" }, ConsoleCode.ERROR); throw new Exception(); }
                    ElementWriter(jt, xml);
                }

            }
            catch
            {
                ConsoleWrite(new string[] { "Failed in CompileIf()" }, ConsoleCode.ERROR); throw;
            }
        }

        private void CompileExpressionList(JackTokenizer jt, XmlWriter xml)
        {
            try
            {
                if (!jt.CurrentToken.Equals(")"))
                {
                    CompileExpression(jt, xml);


                    while (true)
                    {
                        if (jt.PeekToken().Equals(","))
                        {
                            jt.Advance();
                            ElementWriter(jt, xml);

                            jt.Advance();
                            CompileExpression(jt, xml);
                        }
                        else break;
                    }
                }

            }
            catch
            {
                ConsoleWrite(new string[] { "Failed in CompileExpressionList()" }, ConsoleCode.ERROR); throw;
            }
        }

        private void CompileExpression(JackTokenizer jt, XmlWriter xml)
        {
            try
            {
                xml.WriteStartElement("expression");
                CompileTerm(jt, xml);

                while (true)
                {
                    if (JackTokenizer.Operations.Contains(jt.PeekToken()))
                    {
                        jt.Advance();
                        switch (jt.CurrentToken)
                        {
                            case "<":
                                xml.WriteStartElement("symbol");
                                xml.WriteString("&lt");
                                xml.WriteEndElement();
                                break;

                            case ">":
                                xml.WriteStartElement("symbol");
                                xml.WriteString("&gt");
                                xml.WriteEndElement();
                                break;

                            case "&":
                                xml.WriteStartElement("symbol");
                                xml.WriteString("&amp");
                                xml.WriteEndElement();
                                break;

                            default:
                                ElementWriter(jt, xml);
                                break;
                        }

                        jt.Advance();
                        CompileTerm(jt, xml);
                    }
                    else break;
                }

                xml.WriteEndElement();

            }
            catch
            {
                ConsoleWrite(new string[] { "Failed in CompileExpression()" }, ConsoleCode.ERROR); throw;
            }
        }

        private void CompileTerm(JackTokenizer jt, XmlWriter xml)
        {
            try
            {
                xml.WriteStartElement("term");
                
                string token = jt.CurrentToken;

                if (jt.CurrentTokenType == JackTokenizer.TokenType.IDENTIFIER)
                {
                    string peekToken = jt.PeekToken();
                    
                    if (peekToken.Equals("["))
                    {
                        ElementWriter(jt, xml);

                        jt.Advance();
                        ElementWriter(jt, xml);

                        jt.Advance();
                        CompileExpression(jt, xml);

                        jt.Advance();
                        token = jt.CurrentToken;
                        if (!token.Equals("]")) { ConsoleWrite(new string[] { $"Expected closing bracket. Got: {token}" }, ConsoleCode.ERROR); throw new Exception(); }
                        ElementWriter(jt, xml);
                    }
                    else if (peekToken.Equals("(") || peekToken.Equals("."))
                    {
                        CompileCall(jt, xml);
                    }
                    else { ElementWriter(jt, xml); }
                }
                else if (jt.CurrentTokenType == JackTokenizer.TokenType.INT_CONST)
                {
                    ElementWriter(jt, xml);
                }
                else if (jt.CurrentTokenType == JackTokenizer.TokenType.STRING_CONST)
                {
                    ElementWriter(jt, xml);
                }
                else if (token.Equals("this") || token.Equals("null") || token.Equals("true") || token.Equals("false"))
                {
                    ElementWriter(jt, xml);
                }
                else if (token.Equals("("))
                {
                    ElementWriter(jt, xml);

                    jt.Advance();
                    CompileExpression(jt, xml);

                    jt.Advance();
                    token = jt.CurrentToken;
                    if (!token.Equals(")")) { ConsoleWrite(new string[] { $"Expected closing parentheses. Got: {token}" }, ConsoleCode.ERROR); throw new Exception(); }
                    ElementWriter(jt, xml);

                }
                else if (token.Equals("-") || token.Equals("~"))
                {
                    ElementWriter(jt, xml);

                    jt.Advance();
                    CompileTerm(jt, xml);
                }

                xml.WriteEndElement();
            }
            catch
            {
                ConsoleWrite(new string[] { "Failed in CompileTerm()" }, ConsoleCode.ERROR); throw;
            }
        }

    }
}
