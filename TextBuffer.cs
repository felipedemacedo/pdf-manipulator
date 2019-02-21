using System.Text;
using System.Text.RegularExpressions;

namespace Buffers
{
    class TextBuffer
    {
        public StringBuilder Header {get; set;}
        public StringBuilder Body {get; set;}

        public void Init()
        {
            InitHeader();
            InitBody();
        }

        public void InitHeader()
        {
            Header = new StringBuilder();
        }

        public void InitBody()
        {
            Body = new StringBuilder();
        }

        public string GetHeader()
        {
            return Header.ToString();
        }

        public string GetBody()
        {
            return Body.ToString();
        }

        public void AppendToHeader(string str)
        {
            if (Header == null)
                InitHeader();

            if (!str.Trim().Equals(Header.ToString().Trim())) // evitando duplicação de termos no Header
                Header.AppendLine(str.Trim());
        }

        public void AppendToBody(string str)
        {
            if (Body == null)
                InitBody();

            Body.AppendLine(str);
            CleanBody();
        }

        public void CleanBody()
        {
            if (string.IsNullOrEmpty(GetBody()) || GetBody().Equals("\r\n") || GetBody().Equals("\n") || GetBody().Equals("\r"))
            {
                InitBody();
            }
        }

        public void AppendToBegginingOfHeader(string str)
        {
            string newContent = str + Header;
            InitHeader();
            AppendToHeader(newContent);
        }

        public void AppendToBegginingOfBody(string str)
        {
            string newContent = str + Body;
            InitBody();
            AppendToBody(newContent);
        }

        public void AppendToLastLineOfBody(string str)
        {
            string beginning = Regex.Match(GetBody(), @"^.+\r\n(?!$)").ToString();
            string end = GetBody().Replace(beginning, "");
            InitBody();
            AppendToBody(beginning + str + end);
        }

        private void RegexReplacement(int option, string regExpr, string replacement)
        {
            string newContent = string.Empty;
            switch (option)
            {
                case 0: // HEADER
                    newContent = Regex.Replace(Header.ToString(), regExpr, replacement);
                    InitHeader();
                    AppendToHeader(newContent);
                    break;
                case 1: // BODY
                    newContent = Regex.Replace(Body.ToString(), regExpr, replacement);
                    InitBody();
                    AppendToBody(newContent);
                    break;
            }
        }

        public void RegexReplaceOnHeader(string regExpr, string replacement)
        {
            RegexReplacement(0, regExpr, replacement);
        }

        public void RegexReplaceOnBody(string regExpr, string replacement)
        {
            RegexReplacement(1, regExpr, replacement);
        }

        public bool IsEmpty()
        {
            if (!HasContent())
                return true;
            else
                return false;
        }

        public bool HasContent()
        {
            if (!IsEmptyHeader() || !IsEmptyBody())
                return true;
            else
                return false;
        }

        public bool HasContentHeader()
        {
            if (!IsEmptyHeader())
                return true;
            else
                return false;
        }

        public bool HasContentBody()
        {
            if (!IsEmptyBody())
                return true;
            else
                return false;
        }

        public bool IsEmptyHeader()
        {
            if (string.IsNullOrEmpty(Header.ToString()))
                return true;
            else
                return false;
        }

        public bool IsEmptyBody()
        {
            if (string.IsNullOrEmpty(Body.ToString()))
                return true;
            else
                return false;
        }

        public void Flush(ref StringBuilder output)
        {
            if (HasContent())
            {
                // output.AppendLine(Header.ToString().Trim() + " " + Body.ToString().Trim());
                output.AppendLine(Header.ToString().Trim());
                output.AppendLine(Body.ToString().Trim());
            }
            Init();
        }

        public void FlushHeader(ref StringBuilder output)
        {
            if (HasContent())
            {
                // output.AppendLine(Header.ToString().Trim() + " " + Body.ToString().Trim());
                output.AppendLine(Header.ToString().Trim());
            }
            Init();
        }

        public void FlushBody(ref StringBuilder output)
        {
            if (HasContent())
            {
                output.AppendLine(Body.ToString().Trim());
            }
            Init();
        }
    }
}