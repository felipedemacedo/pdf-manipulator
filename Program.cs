using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace TRT
{
    class Program
    {
        static void Main(string[] args)
        {            
            StringBuilder input = ExtractAllText("TST_150219.pdf");
            StringBuilder output = new StringBuilder();
            String[] lines = input.ToString().Split('\n');
            StringBuilder bufferHeader = new StringBuilder();
            StringBuilder bufferBody = new StringBuilder();
            bool bufferEmPreenchimento = false;
            // int numCaso = 0;

            foreach (var line in lines)
            {
                if (Corrige(line, ref bufferEmPreenchimento, ref bufferHeader, ref bufferBody, output, "^TERCEIRO ", @"^\s*INTERESSADO\s*$", "TERCEIRO \nINTERESSADO", "TERCEIRO "))
                    continue;

                if (Corrige(line, ref bufferEmPreenchimento, ref bufferHeader, ref bufferBody, output, @"^AGRAVANTE\s*\(S\)\s*E\s+", @"^\s*AGRAVADO\s*\(S\)\s*$", "AGRAVANTE (S) E\nAGRAVADO (S)", "AGRAVANTE(S) E "))
                    continue;

                output.AppendLine(RemoveWhiteSpaces(line.ToString()));
                System.Console.WriteLine("Content: [" + RemoveWhiteSpaces(line.ToString()) + "]");
            }

            using (StreamWriter sw = new StreamWriter(File.Open("C:\\TRT\\output.txt", FileMode.Create), Encoding.UTF8))
            {
                sw.WriteLine(output.ToString().Trim());
            }
        }

        private static bool Corrige(string line, ref bool bufferEmPreenchimento, ref StringBuilder bufferHeader, ref StringBuilder bufferBody, StringBuilder output, string firstMatch, string secondMatch, string firstFix, string secondFix)
        {
            if (Regex.IsMatch(line, @"^\s+$") || Regex.IsMatch(line, @"^\s*\r\s*$")) // IGNORE ALL BLANK LINES
            {
                return true;
            }
            else if (Regex.IsMatch(line, firstMatch, RegexOptions.IgnoreCase))      // TERCEIRO DEMOCRATES SALES BARBOSA
            {                                                                       // INTERESSADO
                bufferEmPreenchimento = true;
                bufferHeader.Append(firstFix);                                      //  TERCEIRO INTERESSADO
                bufferBody.Append(line.Replace(secondFix,""));                      //  DEMOCRATES SALES BARBOSA
                return true;
            }
            else if (bufferEmPreenchimento && Regex.IsMatch(line, secondMatch, RegexOptions.IgnoreCase))
            {
                output.AppendLine(bufferHeader.ToString());
                System.Console.WriteLine("Content: [" + bufferHeader.ToString() + "]");
                output.AppendLine(bufferBody.ToString());
                System.Console.WriteLine("Content: [" + bufferBody.ToString() + "]");
                bufferEmPreenchimento = false;
                bufferHeader = new StringBuilder();
                bufferBody = new StringBuilder();
                return true;
            }
            return false;
        }

        private static StringBuilder ExtractAllText(string pdf)
        {
            PdfReader reader = new PdfReader(pdf);
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(GetColumnText(reader, 1, 20, 30, 290, 660));  // Página 1 (COLUNA ESQUERDA):
            sb.AppendLine(GetColumnText(reader, 1, 300, 30, 600, 660)); // Página 1 (COLUNA DIREITA):

            for (int i = 2; i < reader.NumberOfPages; i++)
            {
                sb.AppendLine(GetColumnText(reader, i, 20, 30, 290, 774));  // Página 2 em diante (COLUNA ESQUERDA):
                sb.AppendLine(GetColumnText(reader, i, 300, 30, 600, 774)); // Página 2 em diante (COLUNA DIREITA):
            }

            return sb;
        }

        public static string RemoveWhiteSpaces(string str)
        {
            return HttpUtility.HtmlDecode(Regex.Replace(str, " {2,}|^ +", ""));
        }

        private static string GetColumnText(PdfReader reader, int pageNum, float llx, float lly, float urx, float ury)
        {
            // reminder, parameters are in points, and 1 in = 2.54 cm = 72 points
            var rect = new iTextSharp.text.Rectangle(llx, lly, urx, ury);
            var renderFilter = new RenderFilter[1];
            renderFilter[0] = new RegionTextRenderFilter(rect);
            var textExtractionStrategy = new FilteredTextRenderListener(new LocationTextExtractionStrategy(), renderFilter);
            var text = PdfTextExtractor.GetTextFromPage(reader, pageNum, textExtractionStrategy);
            return text;
        }
    }
}