//MIT, 2016-present ,WinterDev
using System;
using System.Text;
namespace BridgeBuilder
{

    class CodeStringBuilder
    {
        StringBuilder _stbuilder = new StringBuilder();
#if DEBUG
        static int _dbugLineCount;
        bool _dbugEnableLineNote = true;
#endif
        public void EnterIndentLevel()
        {

        }
        public void ExitIndentLevel()
        {

        }
        public void AppendLine(string text)
        {
#if DEBUG
            dbugIncLineCount();
#endif
            _stbuilder.AppendLine(text);
        }
        public void AppendLine()
        {
#if DEBUG
            dbugIncLineCount();
#endif
            _stbuilder.AppendLine();
        }
#if DEBUG
        void dbugIncLineCount()
        {

            _dbugLineCount++;
            if (_dbugEnableLineNote)
            {
                if (_dbugLineCount == 322)
                {

                }
                _stbuilder.Append("/*x" + _dbugLineCount + "*/");
                //if (_dbugLineCount >= 14863)
                //{

                //}
            }

        }
#endif

        public void Append(string text) => _stbuilder.Append(text);

        public override string ToString() => _stbuilder.ToString();

    }


}