//MIT, 2019-present ,WinterDev
using System;
using System.Collections.Generic;
using System.Text;
namespace BridgeBuilder
{
    class CDocumentComment
    {
        public string Brief { get; set; } = "";
        public List<CDocumentCommentMethodParam> Parameters = new List<CDocumentCommentMethodParam>();
        public string ReturnNote { get; set; }
        public string InGroup { get; set; }
        public string ThreadSaftyNote { get; set; }
#if DEBUG
        public override string ToString()
        {
            return Brief;
        }
#endif
    }

    class CDocumentCommentMethodParam
    {
        public bool IsOutParameter { get; set; }
        public string ParameterName { get; set; }
        public string Note { get; set; }

#if DEBUG
        public override string ToString()
        {
            StringBuilder stbuilder = new StringBuilder();
            if (IsOutParameter)
            {
                stbuilder.Append("out ");
            }
            if (ParameterName != null)
            {
                stbuilder.Append(ParameterName);
                stbuilder.Append(' ');
            }
            if (Note != null)
            {
                stbuilder.Append(Note);
            }
            return stbuilder.ToString();
        }
#endif
    }

}