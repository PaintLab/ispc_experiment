﻿//MIT, 2016-present ,WinterDev

namespace BridgeBuilder
{

    enum TokenKind : byte
    {
        Id,
        LiteralNumber,
        LiteralString,
        Operator,
        Punc,
        Keyword,
        Comment,
        LineComment,
        Whitespace,
        PreprocessingDirective,
        NewLine
    }
    enum TokenKeyWord : byte
    {

    }

    class Token
    {
        public string Content;
        public TokenKind TokenKind;
        public bool NumberInHexFormat;

        public int LineNo;
#if DEBUG
        static int dbugTotalId;
        public readonly int dbugId = dbugTotalId++;

#endif
        public Token()
        {
#if DEBUG
            //if (dbugId >= 1677)
            //{

            //}
#endif
        }
        public override string ToString() => Content;
    }

}