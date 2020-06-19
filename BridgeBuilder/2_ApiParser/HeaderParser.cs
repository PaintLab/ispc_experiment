//MIT, 2016-present ,WinterDev
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
namespace BridgeBuilder
{


    /// <summary>
    /// header parser
    /// </summary>
    class HeaderParser
    {
        List<string> _allLines = new List<string>();
        List<Token> _tokenList = new List<Token>();
        int _lineNo = -1;
        int _currentTokenIndex;
        CodeCompilationUnit _cu;
        List<Token> _lineComments = new List<Token>(); //before each type/type members
        CodeTypeTemplateNotation _templateNotation = null;

#if DEBUG
        string dbugCurrentFilename = null;
#endif
        public HeaderParser()
        {
        }
        public CodeCompilationUnit Result => _cu;

        public void Parse(string filename)
        {
#if DEBUG
            this.dbugCurrentFilename = Path.GetFileName(filename);
#endif
            _cu = new CodeCompilationUnit(Path.GetFileNameWithoutExtension(filename));
            _cu.Filename = filename;
            using (FileStream fs = new FileStream(filename, FileMode.Open))
            using (StreamReader r = new StreamReader(fs))
            {
                string line = r.ReadLine();
                while (line != null)
                {
                    _allLines.Add(line);
                    line = r.ReadLine();
                }
            }
            //------------
            //start parse line-by-line
            //------------
            ParseFileContent();
            //------------  
        }
        public void Parse(string filename, IEnumerable<string> lines)
        {
#if DEBUG
            this.dbugCurrentFilename = Path.GetFileName(filename);
#endif
            _allLines.AddRange(lines);
            //
            _cu = new CodeCompilationUnit(Path.GetFileNameWithoutExtension(filename));
            _cu.Filename = filename;
            //------------
            //start parse line-by-line
            //------------
            ParseFileContent();
            //------------  
        }
        void ReadUntilEscapeFromBlock()
        {
            int openBraceCount = 0;
            int tkcount = _tokenList.Count;
            for (int n = _currentTokenIndex + 1; n < tkcount; ++n)
            {
                Token nextTk = _tokenList[n];
                if (nextTk.TokenKind == TokenKind.Punc)
                {
                    if (nextTk.Content == "}")
                    {
                        //found , just stop here
                        if (openBraceCount == 0)
                        {
                            _currentTokenIndex = n;
                            break;
                        }
                        else
                        {
                            openBraceCount--;
                        }
                    }
                    else if (nextTk.Content == "{")
                    {
                        openBraceCount++;
                        continue;
                    }

                }
                else
                {
                    continue;
                }
            }

        }
        void ReadUntilEscapeFromInlineComment(Token commentTk)
        {
            StringBuilder stbuilder = new StringBuilder();
            stbuilder.Append(commentTk.Content);

            _lineComments.Add(commentTk);

            int tkcount = _tokenList.Count;
            for (int n = _currentTokenIndex + 1; n < tkcount; ++n)
            {
                Token nextTk = _tokenList[n];
                stbuilder.Append(nextTk);

                if (nextTk.TokenKind == TokenKind.Comment)
                {
                    //found , just stop here
                    _currentTokenIndex = n;
                    //ok
                    commentTk.Content = "/// " + stbuilder.ToString();
                    break;
                }
                else
                {
                    break;
                    //continue;
                }
            }
        }

        static readonly char[] s_ws_seps = new char[] { ' ' };
        DefineConst ParseDefine(string content)
        {
            content = content.Trim();
            int firstWs = content.IndexOf(' ');
            if (firstWs < 0)
            {
                DefineConst defineConst = new DefineConst();
                defineConst.Name = content;
                defineConst.Value = "";
                return defineConst;
            }
            else
            {
                string name = content.Substring(0, firstWs);
                string value = content.Substring(firstWs + 1);
                DefineConst defineConst = new DefineConst();
                defineConst.Name = name.Trim();
                defineConst.Value = value.Trim();
                return defineConst;
            }

        }
        string ParseIncludeFile(Token includePreprocessingDirective)
        {
            string line = includePreprocessingDirective.Content;
            string[] parts = line.Split(' ');
            if (parts.Length == 2 && parts[0] == "#include")
            {
                return parts[1];
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        Token[] FlushCollectedLineComments()
        {
            Token[] comments = null;
            if (_lineComments.Count > 0)
            {
                comments = _lineComments.ToArray();
                _lineComments.Clear();
            }
            return comments;
        }

#if DEBUG
        int _dbugParseStep;
#endif
        void ParseFileContent()
        {
            LineLexer lineLexer = new LineLexer();
            _tokenList.Clear();
            //-------------------------------------------------------
            //[1] Lex
            //-------------------------------------------------------
            lineLexer.Lex(_allLines);
            _tokenList.AddRange(lineLexer._tklist);
#if DEBUG

#endif

            //[2] Parse
            int tkcount = _tokenList.Count;
            CodeTypeDeclaration globalTypeDecl = _cu.GlobalTypeDecl;
            for (_currentTokenIndex = 0; _currentTokenIndex < tkcount; ++_currentTokenIndex)
            {

#if DEBUG
                _dbugParseStep++;
                if (_dbugParseStep == 375)
                {

                }
#endif


                Token tk = _tokenList[_currentTokenIndex];
                switch (tk.TokenKind)
                {
                    default:
                        break;
                    case TokenKind.Whitespace:
                        break;
                    case TokenKind.LineComment:
                        //line comment
                        _lineComments.Add(tk);

                        break;
                    case TokenKind.PreprocessingDirective:
                        {
                            _lineComments.Clear();
                            //this version we just skip some pre-processing 
                            if (tk.Content.StartsWith("#include"))
                            {
                                _cu.AddIncludeFile(ParseIncludeFile(tk));
                            }
                            else if (tk.Content.StartsWith("#define"))
                            {
                                _cu.AddDefineConst(ParseDefine(tk.Content.Substring(7)));
                            }
                        }
                        continue;
                    case TokenKind.Comment:
                        //skip until find next comment
                        //TODO: review here 
                        //not correct
                        //currentTokenIndex++;
                        //ReadUntilEscapeFromInlineComment(tk);
                        _lineComments.Add(tk);
                        continue;//go read next again  
                    case TokenKind.Id:
                        {
                            //may be keyword or iden
                            switch (tk.Content)
                            {
                                case "class":
                                case "struct":
                                    {
                                        //parse class
                                        Token[] comments = FlushCollectedLineComments();
                                        CodeTypeDeclaration typeDecl = ParseTypeDeclaration(tk.Content == "class" ? TypeKind.Class : TypeKind.Struct);
                                        typeDecl.Kind = (tk.Content == "class") ? TypeKind.Class : TypeKind.Struct;
                                        _templateNotation = null;
                                        typeDecl.LineComments = comments;

                                        if (typeDecl != null)
                                        {
                                            _cu.AddTypeDeclaration(typeDecl);
                                        }
                                        else
                                        {
                                            throw new NotSupportedException();
                                        }
                                    }
                                    break;
                                case "enum":
                                    {
                                        CodeTypeDeclaration enumDecl = ParseEnumDeclaration();
                                        string enum_name = ExpectId();
                                        if (enum_name != null)
                                        {
                                            enumDecl.Name = enum_name;
                                        }
                                        else
                                        {
                                            //no name, use auto name
                                            enumDecl.Name = "enum" + globalTypeDecl.MemberCount;
                                        }
                                        //
                                        if (ExpectPunc(";"))
                                        {
                                            globalTypeDecl.AddMember(enumDecl);
                                        }
                                        else
                                        {
                                            throw new NotSupportedException();
                                        }
                                    }
                                    break;
                                case "typedef":
                                    {
                                        //parse typedef 
                                        ParseCTypeDef(globalTypeDecl);
                                    }
                                    break;
                                case "template":
                                    {
                                        //skip template 
                                        _templateNotation = ParseTemplateNotation();
                                    }
                                    break;
                                case "extern":
                                    {
                                        if (ExpectLiteralString("\"C\""))
                                        {
                                            if (ExpectPunc("{"))
                                            {
                                                //ReadUntilEscapeFromBlock();
                                            }
                                            else
                                            {
                                                throw new NotSupportedException();
                                            }
                                        }
                                    }
                                    break;
                                case "namespace":
                                    {
                                        //parse namespace
                                        ParseNamespace(globalTypeDecl);
                                    }
                                    break;

                                default:
                                    {
                                        //may be global func
                                        //should be method
                                        _currentTokenIndex--;
                                        ParseTypeMember(globalTypeDecl);
                                    }
                                    break;
                            }
                        }
                        break;
                }

            }
            //-------------------------------------------------------
        }
        string ExpectId()
        {
            //read next and 
            int i = _currentTokenIndex + 1;
            Token tk = _tokenList[i];
            switch (tk.TokenKind)
            {
                case TokenKind.LineComment:
                    _lineComments.Add(tk);
                    _currentTokenIndex++;
                    return ExpectId();
                case TokenKind.PreprocessingDirective:

                    _currentTokenIndex++;
                    return ExpectId();
                case TokenKind.Comment:
                    _currentTokenIndex++;
                    ReadUntilEscapeFromInlineComment(tk);
                    return ExpectId();
                case TokenKind.Id:
                    _currentTokenIndex = i;
                    return tk.Content;
                default:
                    return null;
            }
        }

        bool ExpectToken(TokenKind k, string value)
        {
            int i = _currentTokenIndex + 1;
            Token tk = _tokenList[i];
            switch (tk.TokenKind)
            {
                case TokenKind.LineComment:
                    _lineComments.Add(tk);
                    _currentTokenIndex++;
                    return ExpectToken(k, value);
                case TokenKind.PreprocessingDirective:
                    _currentTokenIndex++;
                    return ExpectToken(k, value);
                case TokenKind.Id:
                    if (value == null)
                    {
                        //we ask for any Iden token
                        //not test the value
                        _currentTokenIndex++;
                        return true;
                    }
                    else
                    {
                        if (tk.Content == value)
                        {
                            _currentTokenIndex++;
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                case TokenKind.Comment:
                    //found open comment
                    _currentTokenIndex++;
                    ReadUntilEscapeFromInlineComment(tk);
                    return ExpectToken(k, value);
                default:

                    if (tk.TokenKind == k)
                    {
                        if (value != null)
                        {
                            //test the value too
                            if (value == tk.Content)
                            {
                                //value matched with the content
                                _currentTokenIndex++;
                                return true;
                            }
                            else
                            {
                                //same kind but not match with the expect value
                                return false;
                            }
                        }
                        else
                        {
                            //not need test value
                            _currentTokenIndex++;
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
            }
        }
        bool ExpectId(string id)
        {
            return ExpectToken(TokenKind.Id, id);
        }
        bool ExpectLiteralString(string id)
        {
            return ExpectToken(TokenKind.LiteralString, id);
        }
        bool ExpectLiternalNumber(string value)
        {
            //read next and 
            return ExpectToken(TokenKind.LiteralNumber, value);
        }
        bool ExpectId(out Token tk)
        {
            if (ExpectToken(TokenKind.Id, null))
            {
                tk = _tokenList[_currentTokenIndex];
                return true;
            }
            else
            {
                tk = null;
                return false;
            }
        }
        bool ExpectLiternalNumber(out Token tk)
        {
            //read next and 
            if (ExpectToken(TokenKind.LiteralNumber, null))
            {
                tk = _tokenList[_currentTokenIndex];
                return true;
            }
            else
            {
                tk = null;
                return false;
            }
        }
        Token ExpectPunc()
        {
            int i = _currentTokenIndex + 1;
            if (i >= _tokenList.Count)
            {
                return null;
            }
            Token tk = _tokenList[i];
            switch (tk.TokenKind)
            {
                default:
                    return null;
                case TokenKind.Punc:
                    _currentTokenIndex = i;
                    return tk;
            }
            //
        }


        bool ExpectPunc(string expectedPunc)
        {
            //read next and 
            int i = _currentTokenIndex + 1;
            if (i >= _tokenList.Count)
            {
                return false;
            }

            Token tk = _tokenList[i];
            switch (tk.TokenKind)
            {
                case TokenKind.LineComment:
                    _lineComments.Add(tk);
                    _currentTokenIndex++;
                    return ExpectPunc(expectedPunc);
                case TokenKind.PreprocessingDirective:
                    _currentTokenIndex++;
                    return ExpectPunc(expectedPunc);
                case TokenKind.Comment:
                    _currentTokenIndex++;
                    ReadUntilEscapeFromInlineComment(tk);
                    return ExpectPunc(expectedPunc);
                case TokenKind.Punc:
                    if (tk.Content == expectedPunc)
                    {
                        _currentTokenIndex = i;
                        return true;
                    }
                    return false;
            }
            return false;
        }

        bool ExpectPuncSeq(string p1, string p2, string p3)
        {
            return ExpectPunc(p1) && ExpectPunc(p2) && ExpectPunc(p3);
        }
        bool ExpectPuncSeq(string p1, string p2)
        {
            return ExpectPunc(p1) && ExpectPunc(p2);
        }


        MemberAccessibility _currentMemberAccessibilityMode;

        CodeTypeDeclaration ParseTypeDeclaration(TypeKind typeKind)
        {
            //class name

            MemberAccessibility prev_accessibility = _currentMemberAccessibilityMode; //save
            var codeTypeDecl = new CodeTypeDeclaration();
            if (_templateNotation != null)
            {
                codeTypeDecl.TemplateNotation = _templateNotation;
                _templateNotation = null;//reset
            }

            codeTypeDecl.Name = ExpectId();

            if (ExpectPunc(";"))
            {
                //forward decl
                codeTypeDecl.IsForwardDecl = true;
                return codeTypeDecl;
            }

            if (ExpectPunc(":"))
            {
                //expected base class list
                //base modifier
                codeTypeDecl.BaseIsPublic = ExpectId("public");
                codeTypeDecl.BaseIsVirtual = ExpectId("virtual");
                codeTypeDecl.BaseTypes.Add(ExpectType());
            }

            List<string> par_types = null;
            if (ExpectPunc("<"))
            {
                //C++ template TypeParameter
                //(analogous to C# generic)
                par_types = new List<string>();

                string typeParName = ExpectId();
                //int typeParCount = 0;
                while (typeParName != null)
                {

                    par_types.Add(typeParName);
                    if (ExpectPunc(","))
                    {
                        typeParName = ExpectId();
                    }
                    else if (ExpectPunc(">"))
                    {
                        break;
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }

            }

            //-----------------------------------------------------
            if (ExpectPunc("{"))
            {
                //struct detail
                if (par_types != null)
                {
                    //rename 
                    int typeParCount = par_types.Count;
                    for (int i = 0; i < typeParCount; ++i)
                    {
                        codeTypeDecl.AddTypeParameter(new CodeTemplateTypeParameter(par_types[i]));
                    }
                }
                while (ParseTypeMember(codeTypeDecl)) ;
                if (ExpectPunc(";"))
                {
                    //ok
                }
            }
            else if (ExpectPunc("::")) //
            {

                if (typeKind == TypeKind.Struct)
                {
                    //this is not base of struct  
                    string structName = ExpectId();
                    //change current codeTypeDecl name to parent name

                    if (par_types != null)
                    {
                        CodeTypeTemplateTypeReference ss = new CodeTypeTemplateTypeReference(codeTypeDecl.Name);
                        int count = 0;
                        foreach (string par_t in par_types)
                        {
                            ss.AddTemplateItem(new CodeSimpleTypeReference(par_t));
                        }
                        codeTypeDecl.OwnerTypeDecl = ss;
                    }
                    else
                    {
                        CodeSimpleTypeReference ss = new CodeSimpleTypeReference(codeTypeDecl.Name);
                        codeTypeDecl.OwnerTypeDecl = ss;
                    }
                    codeTypeDecl.Name = structName;
                }
                else
                {
                    throw new NotSupportedException();
                }



                if (ExpectPunc("{"))
                {

                    while (ParseTypeMember(codeTypeDecl)) ;
                    if (ExpectPunc(";"))
                    {
                        //ok
                    }
                }

            }

            else
            {

            }

            //------
            _currentMemberAccessibilityMode = prev_accessibility; //restore back
            return codeTypeDecl;
        }

        CodeTemplateParameter ParseTemplateParameter()
        {
            if (!ExpectId("class"))
            {
                //class
            }
            string id_name = ExpectId();
            //this version supports 1 template parameter
            CodeTypeTemplateNotation typeTemplateNotation = new CodeTypeTemplateNotation();
            var templatePar = new CodeTemplateParameter();
            templatePar.ParameterKind = "class";
            templatePar.ParameterName = id_name;
            return templatePar;
        }
        CodeTypeTemplateNotation ParseTemplateNotation()
        {
            if (!ExpectPunc("<"))
            {
                throw new NotSupportedException();
            }

            CodeTypeTemplateNotation typeTemplateNotation = new CodeTypeTemplateNotation();

            CodeTemplateParameter templatePar = ParseTemplateParameter();
            while (templatePar != null)
            {
                typeTemplateNotation.AddTemplateParameter(templatePar);
                Token tk = ExpectPunc();
                if (tk == null)
                {
                    throw new NotSupportedException();
                }

                if (tk.Content == ",")
                {
                    //parse next template arg 
                    templatePar = ParseTemplateParameter();
                }
                else if (tk.Content == ">")
                {
                    break;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return typeTemplateNotation;
        }
        bool ParseNamespace(CodeTypeDeclaration currentTypeDecl)
        {
            string namespaceName = ExpectId(); //may be null
            Token ns_tk = _tokenList[_currentTokenIndex];

            if (!ExpectPunc("{"))
            {
                throw new NotSupportedException();
            }
            //we create a typedecl for namespace 
            CodeTypeDeclaration namespace_as_type = new CodeTypeDeclaration();
            namespace_as_type.Kind = TypeKind.Namespace;
            namespace_as_type.Name = namespaceName ?? "";
            namespace_as_type.StartAtLine = ns_tk.LineNo;

            currentTypeDecl.AddMember(namespace_as_type);

            while (ParseTypeMember(namespace_as_type)) ;


            return true;
        }

        int _dbugStep = 0;
        bool ParseCTypeDef(CodeTypeDeclaration codeTypeDecl)
        {
#if DEBUG
            _dbugStep++;
            if (_dbugStep == 375)
            {

            }
#endif
            Token[] comments = FlushCollectedLineComments();


            CodeTypeReference from = ExpectType();
            string to = "";
            if (codeTypeDecl.TemplateNotation != null)
            {
                if (from.Name == "typename")
                {

                    //template <class traits>
                    //class CefStructBase : public traits::struct_type {
                    // public:
                    //  typedef typename traits::struct_type struct_type; 
                    CodeTemplateParameter par = codeTypeDecl.TemplateNotation.templatePars[0];//?
                    //assign to template parameter 
                    par.TemplateDetailFrom = ExpectType();
                    par.ReAssignToTypeName = ExpectId();
                    //for template
                    if (!ExpectPunc(";"))
                    {
                        throw new NotSupportedException();
                    }
                    return true;
                }
            }

            if (from is CodeFunctionPointerTypeRefernce)
            {
                var f = from as CodeFunctionPointerTypeRefernce;
                f.LocalFuncTypeDecl.LineComments = comments;
                codeTypeDecl.AddMember(f.LocalFuncTypeDecl);
                return true;
            }

            if (from.Name == "enum")
            {
                CodeTypeDeclaration enumDecl = ParseEnumDeclaration();
                enumDecl.LineComments = comments;
                string enum_name = ExpectId();
                if (enum_name != null)
                {
                    enumDecl.Name = enum_name;
                    if (ExpectPunc(";"))
                    {
                        codeTypeDecl.AddMember(enumDecl);
                        return true;
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else if (from.Name == "struct")
            {
                CodeTypeDeclaration struct_decl = ParseTypeDeclaration(TypeKind.Struct);
#if DEBUG
                if (struct_decl.Name == "GLFWimage")
                {

                }
#endif
                struct_decl.Kind = TypeKind.Struct;
                codeTypeDecl.AddMember(struct_decl);
                //


                string typedef_anotherName = ExpectId();
                if (typedef_anotherName != null)
                {
                    if (ExpectPunc(";"))
                    {

                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }

                codeTypeDecl.AddMember(
                    new CodeCTypeDef(
                        new CodeSimpleTypeReference(struct_decl.Name), typedef_anotherName)
                    { LineComments = comments, MemberAccessibility = _currentMemberAccessibilityMode }
                );
                return true;
            }
            else if (from.Name == "void")
            {
                //delegate???
                CodeFunctionPointerTypeDecl funcTypeDecl = new CodeFunctionPointerTypeDecl();
                CodeSimpleTypeReference retType = new CodeSimpleTypeReference("void");
                ExpectPunc("(");
                if (ExpectPunc("*"))
                {
                    funcTypeDecl.Name = ExpectId();
                    funcTypeDecl.ReturnTypeRef = retType;
                }
                ExpectPunc(")");


                //parse parameters
                if (ExpectPunc("("))
                {

                    int parCount = 0;
                NEXT_PAR:
                    bool isConst = ExpectId("const");

                    CodeTypeReference parType = ExpectType();
                    CodeMethodParameter metPar = new CodeMethodParameter();

                    metPar.ParameterName = "p" + parCount;
                    metPar.IsAutoGenParName = true;

                    metPar.IsConstPar = isConst;
                    metPar.ParameterType = parType;
                    parCount++;

                    funcTypeDecl.Parameters.Add(metPar);

                    if (ExpectPunc(","))
                    {
                        goto NEXT_PAR;
                    }
                    else if (ExpectPunc(")"))
                    {
                        //stop
                    }
                    else
                    {
                        metPar.ParameterName = ExpectId();
                        if (metPar.ParameterName != null)
                        {
                            metPar.IsAutoGenParName = false;
                        }
                    }
                }

                funcTypeDecl.MemberAccessibility = _currentMemberAccessibilityMode;
                funcTypeDecl.LineComments = comments;
                codeTypeDecl.AddMember(funcTypeDecl);

                return true;
            }
            else
            {
                throw new NotSupportedException();
            }

            ////------------
            //to = ExpectId();
            //if (!ExpectPunc(";"))
            //{
            //    throw new NotSupportedException();
            //}

            //codeTypeDecl.AddMember(new CodeCTypeDef(from, to) { LineComments = comments, MemberAccessibility = _currentMemberAccessibilityMode });

            //return !ExpectPunc("}");
        }
        CodeExpression ParseExpression()
        {

            Token enum_value = null;
            if (ExpectLiternalNumber(out enum_value))
            {
                //read next token
                Token nextToken1 = ExpectPunc();
                if (nextToken1 != null)
                {
                    switch (nextToken1.Content)
                    {
                        default:
                            throw new NotSupportedException();
                        case ",":
                        case "}":
                        case ")":
                            //retro back
                            _currentTokenIndex--;
                            return new CodeNumberLiteralExpression() { Content = enum_value.ToString() };
                        case ">":
                            {
                                Token nextToken2 = ExpectPunc();
                                if (nextToken2 != null && nextToken2.Content == ">")
                                {
                                    //right shift
                                    //right expr
                                    CodeExpression rightExpr = ParseExpression();
                                    return new CodeBinaryOperatorExpression()
                                    {
                                        LeftExpression = new CodeNumberLiteralExpression() { Content = enum_value.ToString() },
                                        Operator = ">>",
                                        RightExpression = rightExpr
                                    };
                                }
                                else
                                {
                                    _currentTokenIndex--;
                                    return new CodeNumberLiteralExpression() { Content = enum_value.ToString() };
                                }
                            }
                            break;
                        case "<<":
                            {
                                //right expr
                                CodeExpression rightExpr = ParseExpression();
                                return new CodeBinaryOperatorExpression()
                                {
                                    LeftExpression = new CodeNumberLiteralExpression() { Content = enum_value.ToString() },
                                    Operator = nextToken1.Content,
                                    RightExpression = rightExpr
                                };
                            }
                    }
                }
                else
                {
                    return new CodeNumberLiteralExpression() { Content = enum_value.ToString() };
                }
            }
            else
            {
                string enum_value_str = ExpectId();
                //temp only for this version
                return new CodeStringLiteralExpression() { Content = enum_value_str };
            }
        }
        CodeTypeDeclaration ParseEnumDeclaration()
        {
            CodeTypeDeclaration enumDecl = new CodeTypeDeclaration();
            enumDecl.Name = ""; //start with blank -- this may me set later
            enumDecl.Kind = TypeKind.Enum;
            //
            if (!ExpectPunc("{"))
            {
                throw new NotSupportedException();
            }

            //raad filedname
            //
            bool loop = true;
            while (loop)
            {
                string fieldname = ExpectId();
                Token[] comments2 = FlushCollectedLineComments();

                Token next_tk = ExpectPunc();
                if (next_tk != null)
                {
                    switch (next_tk.Content)
                    {
                        default: throw new NotSupportedException();
                        case "}":
                            {
                                //close this enum
                                loop = false;
                            }
                            break;
                        case "=":
                            {
                                //parse simple expression
                                //1. literal num
                                //2. shift expression 
                                CodeFieldDeclaration field_decl = new CodeFieldDeclaration();
                                field_decl.Name = fieldname;
                                field_decl.LineComments = comments2;
                                field_decl.InitExpression = ParseExpression();
                                enumDecl.AddMember(field_decl);
                                fieldname = null;//reset 
                            }
                            break;
                        case ",":
                            {
                                //begin next field 
                                if (fieldname != null)
                                {
                                    CodeFieldDeclaration field_decl = new CodeFieldDeclaration();
                                    field_decl.Name = fieldname;
                                    field_decl.LineComments = comments2;
                                    enumDecl.AddMember(field_decl);
                                    fieldname = null;//reset
                                }

                            }
                            break;
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }

            }


            return enumDecl;
        }
#if DEBUG
        static int dbugCount = 0;
#endif

        void ParseCtorInitializer(CodeMethodDeclaration metDecl)
        {
            string ctor_init_id = ExpectId();
            if (!ExpectPunc("("))
            {
                throw new NotSupportedException();
            }
            //value
            if (ExpectPunc(")"))
            {
                CodeCtorInitilizer ctorInit = new CodeCtorInitilizer();
                ctorInit.initFields.Add(
                    new CodeCtorInitField()
                    {
                        FieldName = ctor_init_id
                    });
                metDecl.CtorInit = ctorInit;
                return;
            }
            //
            CodeExpression exprValue = ParseExpression();
            if (exprValue != null)
            {
                //
                //null expression
                CodeCtorInitilizer ctorInit = new CodeCtorInitilizer();
                ctorInit.initFields.Add(
                    new CodeCtorInitField()
                    {
                        FieldName = ctor_init_id,
                        InitValue = exprValue
                    });
                metDecl.CtorInit = ctorInit;
            }
            else
            {
                //this version=> not support
                throw new NotSupportedException();
            }


            if (!ExpectPunc(")"))
            {
                throw new NotSupportedException();
            }
        }


        bool ParseTypeMember(CodeTypeDeclaration codeTypeDecl)
        {

#if DEBUG
            dbugCount++;
            if (dbugCount == 1)
            {

            }
#endif
            if (ExpectPunc("}"))
            {
                return false;
            }
            //member modifiers
            //this version must be public 
            //parse each member 
            if (ExpectId("public") && ExpectPunc(":"))
            {
                _currentMemberAccessibilityMode = MemberAccessibility.Public;
            }
            else if (ExpectId("private") && ExpectPunc(":"))
            {
                _currentMemberAccessibilityMode = MemberAccessibility.Private;
            }
            else if (ExpectId("protected") && ExpectPunc(":"))
            {
                _currentMemberAccessibilityMode = MemberAccessibility.Protected;
            }
            //---------------------------
            if (ExpectId("typedef"))
            {
                //type def
                return ParseCTypeDef(codeTypeDecl);
            }

            if (ExpectId("extern"))
            {
                //
                //extern member of current type decl , or current namespace
                //
                if (ExpectLiteralString("\"C\""))
                {
                    if (ExpectPunc("{"))
                    {
                        while (ParseTypeMember(codeTypeDecl)) ;

                        return true;
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
            }

            if (ExpectId("friend"))
            {
                //expected friend class decl
                bool isStruct = false;
                if (ExpectId("class"))
                {
                    isStruct = false;
                }
                else if (ExpectId("struct"))
                {
                    isStruct = true;
                }
                else
                {
                    throw new NotSupportedException();
                }
                //--------------------------------------------
                CodeTypeReference friendClassDecl = ExpectType();

                if (!ExpectPunc(";"))
                {
                    throw new NotSupportedException();
                }
                return !ExpectPunc("}");

            }
            CodeTypeReference retType = null;
            //---------------------------
            bool isCefExport = ExpectId("CEF_EXPORT"); //cef specific
            bool isRocksAPI = ExpectId("ROCKSDB_LIBRARY_API");

            if (ExpectId("class"))
            {
                //sub class
                CodeTypeDeclaration subClass = ParseTypeDeclaration(TypeKind.Class);
                subClass.MemberAccessibility = _currentMemberAccessibilityMode;
                subClass.Kind = TypeKind.Class;
                codeTypeDecl.AddMember(subClass);
                return !ExpectPunc("}");
            }
            else if (ExpectId("struct"))
            {
                CodeTypeDeclaration subClass = ParseTypeDeclaration(TypeKind.Struct);
                subClass.MemberAccessibility = _currentMemberAccessibilityMode;
                subClass.Kind = TypeKind.Struct;
                codeTypeDecl.AddMember(subClass);
                //after struct decl

                if (ExpectPunc("*"))
                {
                    retType = new CodePointerTypeReference(
                        new CodeSimpleTypeReference(subClass.Name));

                }
                else
                {
                    return !ExpectPunc("}");
                }
            }

            //modifier
            bool isGLFWAPI = ExpectId("GLFWAPI");
            bool isOperatorMethod = false;
            bool isStatic = ExpectId("static");
            bool isVirtual = ExpectId("virtual");
            bool isConst = ExpectId("const");
            bool isExplicit = ExpectId("explicit");
            bool isMutable = ExpectId("mutable");
            bool isInline = ExpectId("inline");

            bool isDestructor = ExpectPunc("~");

            //-------
            if (retType == null)
            {
                retType = ExpectType();
            }
            bool isCallback = ExpectId("CEF_CALLBACK"); //cef specific
            //-------

            string name = ExpectId();

            if (name == "operator")
            {
                //operator method
                isOperatorMethod = true;
                Token punc = null;
                if ((punc = ExpectPunc()) != null)
                {
                    name = punc.Content;
                }
                else if (ExpectId("delete"))
                {
                    //cpp operator
                    name = "delete";
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            //-----------------------------------

            List<CodeTypeReference> templateMethodTypePars = null;
            CodeTypeReference cppExplicitOwnerTypeName = null;
            if (ExpectPunc("<"))
            {
                //cpp template method (similar to C# generic method)
                templateMethodTypePars = new List<CodeTypeReference>();
                CodeTypeReference templateType = ExpectType();
                while (templateType != null)
                {
                    templateMethodTypePars.Add(templateType);
                    Token tk = ExpectPunc();
                    if (tk == null)
                    {
                        throw new NotSupportedException();
                    }
                    //
                    if (tk.Content == ",")
                    {
                        //parse next template arg 
                        templateType = ExpectType();
                    }
                    else if (tk.Content == ">")
                    {
                        break;
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }

                if (ExpectPunc("::"))
                {
                    //previous part is  cpp's method owner type name 
                    //and 
                    //this is method name 
                    CodeTypeTemplateTypeReference templateTypeRefernece = new CodeTypeTemplateTypeReference(name);
                    foreach (CodeTypeReference typePar in templateMethodTypePars)
                    {
                        templateTypeRefernece.AddTemplateItem(typePar);
                    }
                    cppExplicitOwnerTypeName = templateTypeRefernece;
                    // 
                    name = ExpectId();
                    if (name == "operator")
                    {
                        //operator method
                        isOperatorMethod = true;
                        Token punc = null;
                        if ((punc = ExpectPunc()) != null)
                        {
                            name = punc.Content;
                        }
                        else if (ExpectId("delete"))
                        {
                            //cpp operator
                            name = "delete";
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
                }

            }
            //-----------------------------------
            if (ExpectPunc("("))
            {
                //this may be  method or delegate decl
                Token[] comments = FlushCollectedLineComments();

                if (name == null)
                {
                    //macro/ctor/dtor
                    if (retType.ToString() == codeTypeDecl.Name)
                    {
                        //should be ctor or dtor
                        //this will be handled later
                    }
                    else
                    {
                        //macro or delegate decl
                        //cef3
                        if (!IsAllUpperLetter(retType.ToString()))
                        {
                            //cef 3 : assume this is a func pointer decl
                            //bool isCallback2 = ExpectId("CEF_CALLBACK"); //cef specific
                            //if (!isCallback2)
                            //{
                            //    throw new NotSupportedException();
                            //}
                            //
                            if (!ExpectPunc("*"))
                            {
                                throw new NotSupportedException();
                            }
                            //
                            //pointer field name
                            string delFieldName = ExpectId();

                            CodeFunctionPointerTypeDecl funcPtrTypeDecl = new CodeFunctionPointerTypeDecl();
                            funcPtrTypeDecl.ReturnTypeRef = retType;
                            funcPtrTypeDecl.LineComments = comments;
                            funcPtrTypeDecl.Name = delFieldName;

                            //funcPtrType.ReturnType = retType;
                            //
                            //CodeFieldDeclaration fieldDecl = new CodeFieldDeclaration();
                            //fieldDecl.FieldType = funcPtrType;
                            //fieldDecl.Name = delFieldName;
                            //fieldDecl.LineComments = comments;

                            codeTypeDecl.AddMember(funcPtrTypeDecl);

                            if (!ExpectPunc(")"))
                            {
                                throw new NotSupportedException();
                            }
                            if (!ExpectPunc("("))
                            {
                                throw new NotSupportedException();
                            }
                        //begin pars 
                        //callback ?  
                        AGAIN2:

                            Token tk2 = ExpectPunc();
                            if (tk2 != null)
                            {
                                //stop
                                if (tk2.Content == ",")
                                {
                                    //next par

                                }
                                else if (tk2.Content == ")")
                                {
                                    //end delegate 
                                    if (!ExpectPunc(";"))
                                    {
                                        throw new NotSupportedException();
                                    }

                                    return !ExpectPunc("}");
                                }
                                else
                                {
                                    throw new NotSupportedException();
                                }
                            }

                            CodeMethodParameter metPar = new CodeMethodParameter();
                            bool isConst2 = ExpectId("const");

                            CodeTypeReference parType1 = ExpectType();
                            if (parType1.Name == "struct")
                            {
                                parType1 = ExpectType();
                            }

                            //before par name
                            if (ExpectId("const"))
                            {
                                if (ExpectPunc("*"))
                                {
                                    metPar.IsConstParName = true;
                                }
                                else
                                {

                                }
                            }

                            metPar.ParameterName = ExpectId();
                            metPar.ParameterType = parType1;
                            metPar.IsConstPar = isConst2;

                            funcPtrTypeDecl.Parameters.Add(metPar);
                            goto AGAIN2;

                        }
                    }
                }

                CodeMethodDeclaration met = new CodeMethodDeclaration();
                met.IsStatic = isStatic;
                met.IsVirtual = isVirtual;
                met.IsInline = isInline;
                met.MemberAccessibility = _currentMemberAccessibilityMode;
                met.CppExplicitOwnerType = cppExplicitOwnerTypeName;
                //
                if (retType.ToString() == codeTypeDecl.Name && name == null)
                {
                    //this is ctor or dtor
                    met.ReturnType = null;
                    met.Name = codeTypeDecl.Name;
                    met.MethodKind = (isDestructor) ? MethodKind.Dtor : MethodKind.Ctor;
                }
                else
                {

                    met.IsOperatorMethod = isOperatorMethod;
                    met.Name = name;
                    met.ReturnType = retType;
                }
                met.LineComments = comments;
                //-----------------------------------------------------
                //parse func parameters    

                while (ParseParameter(met.Parameters)) ;

                met.IsOverrided = ExpectId("OVERRIDE");
                met.IsConst = ExpectId("const");


                //cef3:
                //exclude some method like structure, eg. macro
                if (met.Name == null && IsAllUpperLetter(met.ReturnType.Name))
                {
                    codeTypeDecl.AddMacro(met);
                }
                else
                {
                    codeTypeDecl.AddMember(met);
                }
                if (ExpectPunc(";"))
                {
                    //end this 
                    //start new member  
                    return !ExpectPunc("}");
                }

                //---------------- 
                if (met.MethodKind == MethodKind.Ctor && ExpectPunc(":"))
                {
                    //ctor_initializer ...
                    //this version only 1 
                    ParseCtorInitializer(met);
                }
                //----------------


                if (ExpectPunc("{"))
                {
                    Token openBraceTk = _tokenList[_currentTokenIndex];
                    //this version we not parse method body
                    ReadUntilEscapeFromBlock();
                    bool endPunc = !ExpectPunc("}");

                    //----
                    Token closeBraceTk = _tokenList[_currentTokenIndex];
                    met.HasMethodBody = true;
                    met.StartAtLine = openBraceTk.LineNo;
                    met.EndAtLine = closeBraceTk.LineNo;
                    //----
                    return endPunc;
                }
                else if (ExpectPunc("="))
                {
                    if (!ExpectLiternalNumber("0"))
                    {
                        throw new NotSupportedException();
                    }
                    //
                    met.IsAbstract = true;
                    //
                    // no method body
                    if (!ExpectPunc(";"))
                    {
                        throw new NotSupportedException();
                    }
                    return !ExpectPunc("}");
                }
                else
                {
                    return !ExpectPunc("}");
                }

            }
            else if (ExpectPunc(";"))
            {
                Token[] comments = FlushCollectedLineComments();
                //this is code field decl
                CodeFieldDeclaration field = new CodeFieldDeclaration();
                field.MemberAccessibility = _currentMemberAccessibilityMode;
                codeTypeDecl.AddMember(field);
                field.Name = name;
                field.LineComments = comments;
                field.FieldType = retType;
                field.IsStatic = isStatic;
                field.IsConst = isConst;
                return !ExpectPunc("}");
            }
            else if (ExpectPunc("["))
            {
                //array...
                //float arr1[4][4];
                //int32_t arr2[16]; 

                if (ExpectLiternalNumber(out Token arrLen))
                {
                    if (!ExpectPunc("]"))
                    {
                        throw new NotSupportedException();
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }

                CodeArrayReference arrType = new CodeArrayReference(retType, int.Parse(arrLen.Content));

            ARR_RECHECK:
                if (ExpectPunc("["))
                {
                    if (ExpectLiternalNumber(out Token arrLen2))
                    {
                        if (!ExpectPunc("]"))
                        {
                            throw new NotSupportedException();
                        }
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                    CodeArrayReference arr2 = new CodeArrayReference(arrType, int.Parse(arrLen2.Content));
                    arrType = arr2;
                    goto ARR_RECHECK;
                }

                if (!ExpectPunc(";"))
                {
                    throw new NotSupportedException();
                }

                Token[] comments = FlushCollectedLineComments();
                CodeFieldDeclaration field = new CodeFieldDeclaration();
                field.MemberAccessibility = _currentMemberAccessibilityMode;
                codeTypeDecl.AddMember(field);
                field.Name = name;
                field.LineComments = comments;

                field.FieldType = arrType; //**
                field.IsStatic = isStatic;
                field.IsConst = isConst;

                //after array 

                return !ExpectPunc("}");
            }
            else
            {

                Token tk = _tokenList[_currentTokenIndex];
                int lineNo = tk.LineNo;

                throw new NotSupportedException();
            }

        }
        static bool IsAllUpperLetter(string name)
        {

            for (int i = name.Length - 1; i >= 0; --i)
            {
                char c = name[i];
                if (!((c >= 'A' && c <= 'Z') || c == '_'))
                {
                    return false;
                }
            }
            return true;
        }

#if DEBUG
        int dbugExpectTypeCount;
#endif
        CodeTypeReference ExpectType()
        {
#if DEBUG
            dbugExpectTypeCount++;
            if (dbugExpectTypeCount == 536)
            {

            }

#endif

            //TODO: review isConst here

            string typeName = ExpectId();

            if (typeName == "unsigned")
            {
                //this need another id
                string unsignedType = ExpectId();
                if (unsignedType == null)
                {
                    throw new NotSupportedException();
                }
                typeName = "unsigned " + unsignedType;
            }
            else if (typeName == "long")
            {
                if (ExpectId("long"))
                {
                    //this is long-long
                    typeName = "int64";
                }
                else
                {
                    throw new NotSupportedException();
                }
            }


            if (typeName == null) throw new NotSupportedException();
            //-----------------------------------------------------
            if (typeName == "typename")
            {
                //this should be return type
                CodeTypeReference typename = ExpectType();//1. 
                if (ExpectPunc("::"))
                {
                    //CodeTypeReference rightPart = ExpectType();

                    string typename1 = ExpectId();
                    if (ExpectPunc("*"))
                    {
                        //temp fix
                        CodeTypeReference type_n = new CodeQualifiedNameType(typename, new CodeSimpleTypeReference(typename1));
                        return new CodePointerTypeReference(type_n);
                    }
                    else
                    {
                        return new CodeQualifiedNameType(typename, new CodeSimpleTypeReference(typename1));
                    }

                }
                return typename;
            }
            //-----------------------------------------------------

            CodeTypeReference type1 = null;
            //check next token is <
            if (ExpectPunc("<"))
            {
                CodeTypeTemplateTypeReference typeTemplate = new CodeTypeTemplateTypeReference(typeName);

                type1 = typeTemplate;
            //parse each item 
            AGAIN:
                var typeParameter = ExpectType();
                if (typeParameter != null)
                {
                    typeTemplate.AddTemplateItem(typeParameter);
                }
                else
                {
                    throw new NotSupportedException();
                }
                //------------------
                if (ExpectPunc(","))
                {
                    goto AGAIN;
                }
                else if (ExpectPunc(">"))
                {
                    //end
                }
                else if (ExpectPunc("("))
                {
                    //TODO: review here again!!!
                    var funcTypeReference = new CodeFunctionPointerTypeRefernce("");
                    funcTypeReference.ReturnType = typeParameter;
                //callback ?  
                AGAIN2:
                    CodeMethodParameter par = new CodeMethodParameter();
                    par.IsConstPar = ExpectId("const");
                    par.ParameterType = ExpectType();
                    funcTypeReference.Parameters.Add(par);
                    if (ExpectPunc(","))
                    {
                        goto AGAIN2;
                    }
                    else if (ExpectPunc(")"))
                    {
                        //finish
                    }
                    type1 = funcTypeReference;
                    if (!ExpectPunc(">"))
                    {
                        throw new NotSupportedException();
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else if (ExpectPunc("::"))
            {
                return new CodeQualifiedNameType(new CodeSimpleTypeReference(typeName), ExpectType());
            }
            else
            {
                type1 = new CodeSimpleTypeReference(typeName);
            }
        //------------------

        CHECK_AGAIN:
            if (ExpectPunc("*"))
            {
                type1 = new CodePointerTypeReference(type1);
                goto CHECK_AGAIN;
            }
            else if (ExpectPunc("&"))
            {
                type1 = new CodeByRefTypeReference(type1);
                goto CHECK_AGAIN;
            }
            else if (ExpectId("const"))
            {
                //TODO: review here again!
                goto CHECK_AGAIN;
            }

            if (ExpectPunc("("))
            {
                //parse delegate type
                //eg. 
                //extern ROCKSDB_LIBRARY_API void rocksdb_writebatch_iterate(
                //          rocksdb_writebatch_t *, void* state,
                //          void(*put)(void*, const char* k, size_t klen, const char* v, size_t vlen),
                //          void(*deleted)(void*, const char* k, size_t klen));
                var localFuncTypeDecl = new CodeFunctionPointerTypeDecl();

                ////TODO: review here
                //if (typeName != "void" && typeName != "unsigned char")
                //{
                //    //TODO: review here
                //    //WE CHECK only wellknown ret type
                //    throw new NotSupportedException(); //***
                //}

                localFuncTypeDecl.ReturnTypeRef = type1;
                localFuncTypeDecl.Name = "";
                if (ExpectPunc("*"))
                {
                    //local delegate decl here!
                    localFuncTypeDecl.Name = ExpectId();
                }

                if (!ExpectPunc(")"))
                {
                    throw new NotSupportedException();
                }
                //----
                if (ExpectPunc("("))
                {
                    while (ParseParameter(localFuncTypeDecl.Parameters)) ;
                }


                ////parse each parameter
                //int parCount = 0;
                //while (!ExpectPunc(")"))
                //{
                //    CodeMethodParameter par = new CodeMethodParameter();
                //    par.ParameterType = ExpectType();
                //    par.ParameterName = "p" + parCount; //auto-name
                //    parCount++;

                //    if (ExpectId(out Token parameterName))
                //    {
                //        par.ParameterName = parameterName.Content;
                //    }
                //    localFuncTypeDecl.Parameters.Add(par);

                //    if (ExpectPunc(","))
                //    {
                //        //ok
                //    }
                //}
                return new CodeFunctionPointerTypeRefernce(localFuncTypeDecl);
            }

            //------------------
            return type1;
        }

#if DEBUG
        int dbugParseParameter = 0;
#endif
        bool ParseParameter(List<CodeMethodParameter> outputPars)
        {
#if DEBUG
            dbugParseParameter++;
            if (dbugParseParameter == 389)
            {

            }
#endif
            if (ExpectPunc(")"))
            {
                return false;
            }

            var metPar = new CodeMethodParameter();
            metPar.IsConstPar = ExpectId("const");
            if (ExpectId("struct"))
            {
            }
            metPar.ParameterType = ExpectType();
            //--------------------------------
            if (ExpectId("const"))
            {
                metPar.IsConstParName = true;
                if (ExpectPunc("*"))
                {
                    metPar.IsConstPointerParName = true;
                }
            }
            //--------------------------------

            metPar.ParameterName = ExpectId();
            if (metPar.ParameterName == null)
            {
                //use autoname
                metPar.ParameterName = "p" + outputPars.Count;
                metPar.IsAutoGenParName = true;
            }

            outputPars.Add(metPar);

            //after parameter
            if (ExpectPunc(","))
            {
                return true;
            }
            else if (ExpectPunc(")"))
            {
                return false;
            }
            else if (ExpectPunc("["))
            {
                //eg.   CefScopedArgArray(int argc, char* argv[]) {
                //eg.
                //            extern ROCKSDB_LIBRARY_API void rocksdb_set_options(
                //rocksdb_t* db, int count, const char* const keys[], const char* const values[], char** errptr);

                if (ExpectPunc("]"))
                {
                    metPar.ParameterType = new CodeArrayReference(metPar.ParameterType, -1);
                    if (ExpectPunc(","))
                    {
                        return true;
                    }
                    else if (ExpectPunc(")"))
                    {
                        return false;
                    }
                    return true;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

    }

}