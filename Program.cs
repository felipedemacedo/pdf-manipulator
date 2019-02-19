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
    public interface ITextBuffer
    {
        void Init(ref StringBuilder bufferHeader, ref StringBuilder bufferBody);
        bool HasContent(StringBuilder bufferHeader, StringBuilder bufferBody);
        void Flush(ref StringBuilder output, ref StringBuilder bufferHeader, ref StringBuilder bufferBody);
    }
    class TextBuffer : ITextBuffer
    {
        public void Init(ref StringBuilder bufferHeader, ref StringBuilder bufferBody)
        {
            bufferHeader = new StringBuilder();
            bufferBody = new StringBuilder();
        }

        public bool HasContent(StringBuilder bufferHeader, StringBuilder bufferBody)
        {
            if (!string.IsNullOrEmpty(bufferHeader.ToString()) || !string.IsNullOrEmpty(bufferBody.ToString()))   // ambos vazios !
                return true;   // buffer está sendo preenchido
            else
                return false;   // buffer complemtamente vazio
        }

        public void Flush(ref StringBuilder output, ref StringBuilder bufferHeader, ref StringBuilder bufferBody)
        {
            if (HasContent(bufferHeader, bufferBody))
            {
                output.AppendLine(bufferHeader.ToString());
                output.Append(bufferBody.ToString());
            }

            Init(ref bufferHeader, ref bufferBody);
        }
    }

    class Program
    {
        private static int idMatch = 0;
        private static TextBuffer _buffer = new TextBuffer();        
        public static void Init(ref StringBuilder bufferHeader, ref StringBuilder bufferBody)
        {
            ((ITextBuffer)_buffer).Init(ref bufferHeader, ref bufferBody);
        }
        public static bool HasContent(StringBuilder bufferHeader, StringBuilder bufferBody)
        {
            return ((ITextBuffer)_buffer).HasContent(bufferHeader, bufferBody);
        }
        public static void Flush(ref StringBuilder output, ref StringBuilder bufferHeader, ref StringBuilder bufferBody)
        {
            ((ITextBuffer)_buffer).Flush(ref output, ref bufferHeader, ref bufferBody);
        }

        static void Main(string[] args)
        {
            StringBuilder input = ExtractAllText("TST_150219.pdf");
            StringBuilder output = new StringBuilder();
            String[] lines = input.ToString().Split('\n');
            StringBuilder bufferHeader = new StringBuilder();
            StringBuilder bufferBody = new StringBuilder();

            foreach (var line in lines)
            {
                if ( !line.Trim().Equals("") && 
                    (  // IGNORE ALL BLANK LINES
                        // !Corrige(5, line, ref bufferHeader, ref bufferBody, ref output, @"^AGRAVANTE\s*\(S\)\s*,\s+", @"^\s*RECORRENTE\s*\(S\)\s*E\s*", "AGRAVANTE(S),\nRECORRENTE(S) E\nRECORRIDO(A)(S)" ) && 
                        // !Corrige(8, line, ref bufferHeader, ref bufferBody, ref output, @"^AGRAVANTE\(S\),\s+", @"^AGRAVADO\(A\)\(S\)\s+E\s*", "AGRAVANTE(S),\nAGRAVADO(A)(S) E\nRECORRENTE(S)" ) &&
                        !Corrige(6, line, ref bufferHeader, ref bufferBody, ref output, @"^AGRAVADO\(A\)\(S\),\s+", @"^RECORRENTE\(S\)\s+E\s*", "AGRAVADO(A)(S),\nRECORRENTE(S) E\nRECORRIDO(A)(S)" ) &&
                        !Corrige(1, line, ref bufferHeader, ref bufferBody, ref output, @"^TERCEIRO\s+", @"^\s*INTERESSADO\s*$" ) &&
                        !Corrige(2, line, ref bufferHeader, ref bufferBody, ref output, @"^RECORRENTE\s+E\s+", @"^RECORRIDO\s+" ) &&
                        !Corrige(3, line, ref bufferHeader, ref bufferBody, ref output, @"^.+\s*\(S\)\s*E\s+", @"^\s*.+\s*\(S\)" ) &&
                        !Corrige(4, line, ref bufferHeader, ref bufferBody, ref output, @"^.+\s*\(S\)\s*E\s+", @"^\s*.+\s*\(S\)" ) &&
                        !Corrige(7, line, ref bufferHeader, ref bufferBody, ref output, @"^.+\s*\(S\)\s*E\s+", @"^\s*.*\s*\(S\)\s*" ) 
                    )
                )
                {
                    output.AppendLine(line.ToString().Trim());
                }
            }

            // ESCREVE O ARQUIVO TXT FINAL
            using (StreamWriter sw = new StreamWriter(File.Open("C:\\TRT\\output.txt", FileMode.Create), Encoding.UTF8))
            {
                sw.WriteLine(output.ToString().Trim());
            }
        }
        
        private static bool Corrige(int id, string line, ref StringBuilder bufferHeader, ref StringBuilder bufferBody, ref StringBuilder output, string firstMatch, string secondMatch, string headerReplacement = "")
        {
            line = line.Trim();

            if (HasContent(bufferHeader, bufferBody) && id != idMatch)
                return false;

            if (Regex.IsMatch(line, firstMatch))
            {                                                                            
                bufferHeader.AppendLine(Regex.Match(line, firstMatch).ToString().Trim());
                bufferBody.AppendLine(Regex.Replace(line, firstMatch, "").Trim());          
                idMatch = id;
                return true;
            }
            else if (HasContent(bufferHeader, bufferBody) && Regex.IsMatch(line, secondMatch))
            {
                if (!string.IsNullOrEmpty(headerReplacement))
                {
                    bufferHeader = new StringBuilder();
                    bufferHeader.AppendLine(headerReplacement);
                }
                else
                {
                    bufferHeader.AppendLine(Regex.Match(line, secondMatch).ToString().Trim());
                }

                bufferBody.AppendLine(Regex.Replace(line, secondMatch, "").Trim());
                Flush(ref output, ref bufferHeader, ref bufferBody);
                // idMatch = 0;
                return true;
            }
            else
            {
                if (idMatch == 5 || idMatch == 6 || idMatch == 8) // pular a terceira linha do caso 5 (RECORRIDO(A)(S))
                {
                    Init(ref bufferHeader, ref bufferBody);
                    idMatch = 0;
                    return true;    // pular linha
                }

                if (idMatch != 0)
                    return false;

                Flush(ref output, ref bufferHeader, ref bufferBody);   // caso tenha ocorrido o primeiro match mas o segundo não: esvaziar o buffer
                return false;
            }
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