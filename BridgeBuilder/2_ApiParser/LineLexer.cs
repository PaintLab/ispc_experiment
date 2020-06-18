//MIT, 2016-present ,WinterDev
using System;
using System.Collections.Generic;
using System.Text;
namespace BridgeBuilder
{
    class LineLexer
    {
        public List<Token> _tklist = new List<Token>();
        int _currentLineNo = 0;
        public void Lex(List<string> lines)
        {

            _tklist.Clear();
            int begin = 0;
            int end = lines.Count;
            int state = 0;

            Token _latestMultiLineCommentToken = null;

            for (int nn = begin; nn < end; ++nn)
            {
                _currentLineNo = nn;

                string line = lines[nn].TrimStart();

                if (line.StartsWith("//"))
                {
                    //comment
                    _tklist.Add(new Token() { Content = line, TokenKind = TokenKind.LineComment, LineNo = _currentLineNo });
                    continue;
                }
                else if (line.StartsWith("#"))
                {
                    var token = new Token() { Content = line, TokenKind = TokenKind.PreprocessingDirective, LineNo = _currentLineNo };
                    _tklist.Add(token);
                    token.LineNo = nn;
                    while (line.EndsWith("\\"))
                    {
                        //concat
                        //with next line
                        nn++;
                        line = lines[nn];
                        token.Content += ("\r\n" + line);
                    }
                    continue;
                }

                char[] charBuffer = line.ToCharArray();
                int j = charBuffer.Length;


                for (int i = 0; i < j; ++i)
                {
                    char c = charBuffer[i];
                    switch (state)
                    {
                        case 0:
                            {
                                if (char.IsLetter(c) || c == '_')
                                {
                                    LexIden(charBuffer, j, ref i);
                                }
                                else if (char.IsNumber(c))
                                {
                                    LexNumber(charBuffer, '\0', j, ref i);
                                }
                                else if (c == '-')
                                {
                                    //read next
                                    if (i < j - 1)
                                    {
                                        char next_char = charBuffer[i + 1];
                                        if (char.IsNumber(next_char))
                                        {
                                            //consume 
                                            i++;
                                            LexNumber(charBuffer, '-', j, ref i);
                                        }
                                    }
                                }
                                else if (char.IsWhiteSpace(c))
                                {
                                    //whitespace
                                    //skip
                                }
                                else if (c == '"')
                                {
                                    //string literal
                                    LexStringLiteral(charBuffer, j, ref i);
                                }
                                else
                                {
                                    //one or multiple 
                                    state = LexPunc(charBuffer, j, ref i);
                                    if (state == 1)
                                    {
                                        _latestMultiLineCommentToken = _tklist[_tklist.Count - 1];
                                    }
                                }
                            }
                            break;
                        case 1:
                            {
                                //previous line is /* comment
                                if (c == '*' && (i < j - 1) && charBuffer[i + 1] == '/')
                                {
                                    //close comment 

                                    //flush 
                                    _latestMultiLineCommentToken.Content += new string(charBuffer, 0, i + 1);
                                    _latestMultiLineCommentToken = null;
                                    state = 0;
                                    i++;
                                }
                                else
                                {

                                }
                            }
                            break;
                    }
                }
                //end line is essentail
                //-------
                if (state == 1)
                {
                    //flush comment token
                    if (_latestMultiLineCommentToken != null)
                    {
                        _latestMultiLineCommentToken.Content += "\r\n" + new string(charBuffer);
                    }
                }
            }
        }

        public void Lex(string line)
        {
            int state = 0;
            _tklist.Clear();
            char[] charBuffer = line.ToCharArray();
            int j = charBuffer.Length;

            for (int i = 0; i < j; ++i)
            {
                char c = charBuffer[i];
                switch (state)
                {
                    case 0:
                        {
                            if (char.IsLetter(c) || c == '_')
                            {
                                LexIden(charBuffer, j, ref i);
                            }
                            else if (char.IsNumber(c))
                            {
                                LexNumber(charBuffer, '\0', j, ref i);
                            }
                            else if (c == '-')
                            {
                                //read next
                                if (i < j - 1)
                                {
                                    char next_char = charBuffer[i + 1];
                                    if (char.IsNumber(next_char))
                                    {
                                        //consume 
                                        i++;
                                        LexNumber(charBuffer, '-', j, ref i);
                                    }
                                }
                            }
                            else if (char.IsWhiteSpace(c))
                            {
                                //whitespace
                                //skip
                            }
                            else if (c == '"')
                            {
                                //string literal
                                LexStringLiteral(charBuffer, j, ref i);
                            }
                            else
                            {
                                //one or multiple 
                                LexPunc(charBuffer, j, ref i);
                            }
                        }
                        break;
                }
            }
        }
        public void AssignLineNumber(int lineNum)
        {
            //we lex line-by-line,
            //so all token in the list have the same lineNum
            for (int i = _tklist.Count - 1; i >= 0; --i)
            {
                _tklist[i].LineNo = lineNum;
            }
        }
        int LexPunc(char[] charBuffer, int charCount, ref int currentIndex)
        {

            StringBuilder stbuilder = new StringBuilder();
            char c = charBuffer[currentIndex];
            switch (c)
            {
                case ':':
                    // :, ::
                    if (currentIndex + 1 < charCount)
                    {
                        //read next
                        char c1 = charBuffer[currentIndex + 1];
                        if (c1 == ':') // ::
                        {
                            currentIndex += 1;
                            _tklist.Add(
                                new Token() { Content = (c.ToString() + c1.ToString()), TokenKind = TokenKind.Punc, LineNo = _currentLineNo });
                        }
                        else
                        {
                            //just single token
                            _tklist.Add(
                               new Token() { Content = c.ToString(), TokenKind = TokenKind.Punc, LineNo = _currentLineNo });
                        }
                    }
                    else
                    {
                        //just single token
                        _tklist.Add(
                              new Token() { Content = c.ToString(), TokenKind = TokenKind.Punc, LineNo = _currentLineNo });
                    }
                    break;
                case '+':  //++, += 
                case '-':
                    {
                        if (currentIndex + 1 < charCount)
                        {
                            //read next
                            char c1 = charBuffer[currentIndex + 1];
                            if (c1 == '=' || //+=, -=
                                (c == '+' && c1 == '+') || //++
                                (c == '-' && c1 == '-')) //--
                            {
                                currentIndex += 1;
                                _tklist.Add(
                                    new Token() { Content = (c.ToString() + c1.ToString()), TokenKind = TokenKind.Punc, LineNo = _currentLineNo });
                            }
                            else
                            {
                                //just single token
                                _tklist.Add(
                                   new Token() { Content = c.ToString(), TokenKind = TokenKind.Punc, LineNo = _currentLineNo });
                            }
                        }
                        else
                        {
                            //just single token
                            _tklist.Add(
                                  new Token() { Content = c.ToString(), TokenKind = TokenKind.Punc, LineNo = _currentLineNo });
                        }
                    }
                    break;
                case '<':
                    {
                        if (currentIndex + 1 < charCount)
                        {
                            //read next
                            char c1 = charBuffer[currentIndex + 1];
                            if (c1 == '<') //<<
                            {
                                currentIndex += 1;
                                _tklist.Add(
                                    new Token() { Content = (c.ToString() + c1.ToString()), TokenKind = TokenKind.Punc, LineNo = _currentLineNo });
                            }
                            else
                            {
                                //just single token
                                _tklist.Add(
                                   new Token() { Content = c.ToString(), TokenKind = TokenKind.Punc, LineNo = _currentLineNo });
                            }
                        }
                        else
                        {
                            //just single token
                            _tklist.Add(
                                  new Token() { Content = c.ToString(), TokenKind = TokenKind.Punc, LineNo = _currentLineNo });
                        }
                    }
                    break;
                case '=':// ==,  
                case '!'://!=  
                case '%':
                case '^':
                case '~':
                    if (currentIndex + 1 < charCount)
                    {
                        //read next
                        char c1 = charBuffer[currentIndex + 1];
                        if (c1 == '=')
                        {
                            currentIndex += 1;
                            _tklist.Add(
                                new Token() { Content = (c.ToString() + c1.ToString()), TokenKind = TokenKind.Punc, LineNo = _currentLineNo });
                        }
                        else
                        {
                            //just single token
                            _tklist.Add(
                               new Token() { Content = c.ToString(), TokenKind = TokenKind.Punc, LineNo = _currentLineNo });
                        }
                    }
                    else
                    {
                        //just single token
                        _tklist.Add(
                              new Token() { Content = c.ToString(), TokenKind = TokenKind.Punc, LineNo = _currentLineNo });
                    }
                    break;
                case '*':
                    {


                        //may be *=, */
                        if (currentIndex + 1 < charCount)
                        {
                            //read next
                            char c1 = charBuffer[currentIndex + 1];
                            switch (c1)
                            {
                                case '=':
                                    _tklist.Add(
                                        new Token() { Content = (c.ToString() + c1.ToString()), TokenKind = TokenKind.Punc, LineNo = _currentLineNo });
                                    currentIndex += 1;
                                    break;
                                case '/':
                                    //line comment
                                    _tklist.Add(
                                         new Token() { Content = (c.ToString() + c1.ToString()), TokenKind = TokenKind.Comment, LineNo = _currentLineNo });
                                    currentIndex += 1;
                                    break;
                                default:
                                    _tklist.Add(new Token() { Content = c.ToString(), TokenKind = TokenKind.Punc, LineNo = _currentLineNo });
                                    break;
                            }
                        }
                        else
                        {
                            //just single token
                            _tklist.Add(
                                  new Token() { Content = c.ToString(), TokenKind = TokenKind.Punc, LineNo = _currentLineNo });
                        }



                    }
                    break;
                case '/':

                    //may be /=, // , /*
                    if (currentIndex + 1 < charCount)
                    {
                        //read next
                        char c1 = charBuffer[currentIndex + 1];
                        switch (c1)
                        {
                            case '=':
                                _tklist.Add(
                                    new Token() { Content = (c.ToString() + c1.ToString()), TokenKind = TokenKind.Punc, LineNo = _currentLineNo });
                                currentIndex += 1;
                                break;
                            case '*':
                                // /*
                                _tklist.Add(
                                    new Token() { Content = (c.ToString() + c1.ToString()), TokenKind = TokenKind.Comment, LineNo = _currentLineNo });
                                currentIndex += 1;
                                return 1;//***
                            case '/':
                                //line comment
                                _tklist.Add(
                                    new Token() { Content = new string(charBuffer, currentIndex, charCount - currentIndex), TokenKind = TokenKind.LineComment, LineNo = _currentLineNo });
                                currentIndex = charCount; //comsume to end of file
                                break;
                            default:
                                _tklist.Add(new Token() { Content = c.ToString(), TokenKind = TokenKind.Punc, LineNo = _currentLineNo });
                                break;
                        }
                    }
                    else
                    {
                        //just single token
                        _tklist.Add(
                              new Token() { Content = c.ToString(), TokenKind = TokenKind.Punc, LineNo = _currentLineNo });
                    }

                    break;
                default:
                    //single token
                    _tklist.Add(
                              new Token() { Content = c.ToString(), TokenKind = TokenKind.Punc , LineNo = _currentLineNo });
                    break;
            }
            return 0;

        }
        void LexIden(char[] charBuffer, int charCount, ref int currentIndex)
        {
            //lex iden
            StringBuilder stbuilder = new StringBuilder();
            char c = charBuffer[currentIndex];
            stbuilder.Append(c);
            //read next char
            for (int i = currentIndex + 1; i < charCount; ++i)
            {
                c = charBuffer[i];
                if (char.IsLetter(c) || c == '_' || char.IsNumber(c))
                {
                    stbuilder.Append(c);
                    currentIndex = i;
                }
                else
                {
                    //stop
                    //here
                    break;
                }
            }

            if (stbuilder.Length > 0)
            {
                _tklist.Add(new Token() { Content = stbuilder.ToString(), TokenKind = TokenKind.Id, LineNo = _currentLineNo });
            }
        }
        void LexNumber(char[] charBuffer, char signChar, int charCount, ref int currentIndex)
        {
            StringBuilder stbuilder = new StringBuilder();
            char c = charBuffer[currentIndex];
            //
            if (signChar == '-')
            {
                stbuilder.Append('-');
            }
            //
            stbuilder.Append(c);

            bool isHexNum = false;
            for (int i = currentIndex + 1; i < charCount; ++i)
            {
                c = charBuffer[i];
                if (char.IsNumber(c))
                {
                    stbuilder.Append(c);
                    currentIndex = i;
                }
                else if (c == 'x' || c == 'X')
                {
                    //may be hex number
                    //look next
                    if (isHexNum)
                    {
                        throw new NotSupportedException();
                    }
                    stbuilder.Append(c);
                    currentIndex = i;
                    isHexNum = true;

                }
                else
                {
                    //stop
                    //here
                    if (isHexNum)
                    {
                        char c1 = c.ToString().ToLower()[0];
                        if (c1 >= 'a' && c1 <= 'f')
                        {
                            //also hex 
                            stbuilder.Append(c);
                            currentIndex = i;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (stbuilder.Length > 0)
            {
                _tklist.Add(new Token()
                {
                    Content = stbuilder.ToString(),
                    TokenKind = TokenKind.LiteralNumber,
                    NumberInHexFormat = isHexNum,
                    LineNo = _currentLineNo

                });
            }
        }
        void LexStringLiteral(char[] charBuffer, int charCount, ref int currentIndex)
        {
            StringBuilder stbuilder = new StringBuilder();
            char c = charBuffer[currentIndex];
            stbuilder.Append(c);
            for (int i = currentIndex + 1; i < charCount; ++i)
            {
                c = charBuffer[i];
                if (c == '"')
                {
                    currentIndex = i;
                    stbuilder.Append(c);
                    break;
                }
                else if (c == '\\')
                {
                    //escape
                    throw new NotSupportedException();
                }
                else
                {
                    stbuilder.Append(c);
                }
            }

            if (stbuilder.Length > 0)
            {
                _tklist.Add(new Token() { Content = stbuilder.ToString(), TokenKind = TokenKind.LiteralString, LineNo = _currentLineNo });
            }

        }
    }
}