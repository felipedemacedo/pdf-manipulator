using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Buffers;
using System.Collections.Generic;

namespace TRT
{
    class Program
    {
        static void Main(string[] args)
        {
            StringBuilder bookmarks = ExtractAllBookmarks("TST_150219.pdf");
            List<string> inputLines = ExtractAllText("TST_150219.pdf");

            


            System.Console.WriteLine("ok");

            // CASO ESPECIAL: TERCEIRO INTERESSADO
            // - ANTES:    -----------------------------------
            // TERCEIRO DEMOCRATES SALES BARBOSA
            // INTERESSADO
            // - DEPOIS:   -----------------------------------
            // [ HEADER ]: TERCEIRO
            // [ HEADER ]: INTERESSADO
            // [  BODY  ]: DEMOCRATES SALES BARBOSA
            inputLines = Tratamento(1, inputLines, @"(^\s*TERCEIRO\s+)|(^\s*INTERESSADO\s*$)");

            // ---------------------------------------------
            // - ANTES:    -----------------------------------
            // AGRAVADO(S) UNIÃO (PGF)
            // AGRAVADO(S) UNIVERSIDADE FEDERAL DO RIO
            // - DEPOIS:   -----------------------------------
            // [HEADER]: AGRAVADO(S)
            // [ BODY ]: UNIÃO (PGF)
            // [ BODY ]: UNIVERSIDADE FEDERAL DO RIO
            // ---------------------------------------------
            // - ANTES:    -----------------------------------
            // AGRAVANTE(S) E RICARDO FRUTUOSO BORGES
            // RECORRENTE(S)
            // - DEPOIS:   -----------------------------------
            // [ HEADER ]: AGRAVANTE(S) E
            // [ HEADER ]: RECORRENTE(S)
            // [  BODY  ]: RICARDO FRUTUOSO BORGES
            // ---------------------------------------------
            inputLines = Tratamento(1, inputLines, @"^(AGRAVANTE|AGRAVADO|RECORRENTE|RECORRIDO|EMBARGANTE|EMBARGADO)(\s*\([A-Z]\)\s*)*\s*,?\:?\s*(E($|\s+))?", "", true);

            // ---------------------------------------------
            // - ANTES:    -----------------------------------
            // Processo Nº ARR-0001607-40.2015.5.08.0110
            // - DEPOIS:   -----------------------------------
            // ****************************** PROCESSO ******************************
            // ARR-0001607-40.2015.5.08.0110
            // ---------------------------------------------
            inputLines = Tratamento(2, inputLines, @"^Processo\s*N.\s*", "****************************** PROCESSO ******************************");



            WriteIntoFile(ConvertFromLines(inputLines), "C:\\TRT\\output.txt");
        }

        
        private static List<string> Tratamento(int modo, List<string> inputLines, string mainRegex, string replacement="", bool casoEspecial1=false)
        {
            TextBuffer _buffer = new TextBuffer();
            StringBuilder output = new StringBuilder();
            string match = string.Empty;
            int matchsCounter = 0;
            bool isMatch = false;

            foreach (var line in inputLines)
            {
                isMatch = Regex.IsMatch(line, mainRegex);
                if (isMatch)
                {
                    matchsCounter++;
                    match = Regex.Match(line, mainRegex).ToString();
                    switch (modo)
                    {
                        case 1:
                            _buffer.AppendToHeader(match.Trim());                   // o match
                            _buffer.AppendToBody(line.Replace(match, "").Trim());    // o restante da linha sem o match
                            break;
                        case 2:
                            _buffer.AppendToHeader(replacement);                   // o match
                            _buffer.AppendToBody(line.Replace(match, "").Trim());    // o restante da linha sem o match
                            break;
                    }
                }
                else
                {
                    TrataCasoEspecial1(_buffer, matchsCounter, casoEspecial1);

                    if (matchsCounter > 0)   // quebrou sequência de MATCHS
                    {
                        _buffer.Flush(ref output);  // escreve o buffer no arquivo saída
                        matchsCounter = 0;
                    }
                    output.AppendLine(line.ToString().Trim());
                }
            }
            inputLines = ConvertToLines(output);
            return inputLines;
        }

        private static void TrataCasoEspecial1(TextBuffer _buffer, int matchsCounter, bool ativo)
        {
            if (!ativo)
                return;

            if (matchsCounter == 2)  // SEGUNDO MATCH
            {
                if (Regex.IsMatch(_buffer.Header.ToString(), @"\sE\s*$"))   // CASO ESPECIAL: Captura do "E" pertencente ao termo e não ao tópico
                {   // ANTES:
                    // [HEADER]: AGRAVANTE(S) E\r\nRECORRENTE(S) E\r\n
                    // [BODY]: COMPANHIA ESTADUAL DE ÁGUAS\r\nESGOTOS - CEDAE\r\n
                    string replacement = Regex.Match(_buffer.GetHeader(), @"\s*.\s*$").ToString();  // [ E]
                    _buffer.RegexReplaceOnHeader(replacement + "$", "");
                    _buffer.AppendToLastLineOfBody(replacement.Trim() + " ");
                    // DEPOIS:
                    // [HEADER]: AGRAVANTE(S) E\r\nRECORRENTE(S)\r\n
                }   // [BODY]: COMPANHIA ESTADUAL DE ÁGUAS\r\nE ESGOTOS - CEDAE\r\n
            }
        }

        private static string FirstMatch(string input, ref List<string> regexList)
        {
            foreach (var regex in regexList)
            {
                if (Regex.IsMatch(input, regex))
                    return Regex.Match(input, regex).ToString();
            }
            return string.Empty;
        }

        private static bool IsMatch(string input, ref List<string> regexList)
        {
            foreach (var regex in regexList)
            {
                if (Regex.IsMatch(input, regex))
                    return true;
            }
            return false;
        }

        private static List<string> ConvertToLines(StringBuilder inputText, bool removeEmptyLines=true)
        {
            String[] lines = inputText.ToString().Split('\n');
            List<string> lista = new List<string>();
            foreach (var line in lines)
            {
                if (!removeEmptyLines || !line.Trim().Equals(string.Empty))
                    lista.Add(line);
            }
            return lista;
        }

        private static StringBuilder ConvertFromLines(List<string> inputText, bool removeEmptyLines=true)
        {
            StringBuilder lista = new StringBuilder();
            foreach (var line in inputText)
            {
                if (!removeEmptyLines || !line.Trim().Equals(string.Empty))
                    lista.Append(line);
            }
            return lista;
        }

        private static void WriteIntoFile(StringBuilder output, string filePath)
        {
            // ESCREVE O ARQUIVO TXT FINAL
            using (StreamWriter sw = new StreamWriter(File.Open(filePath, FileMode.Create), Encoding.UTF8))
            {
                sw.WriteLine(output.ToString().Trim());
            }
        }

        private static List<string> ExtractAllText(string pdf)
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
            return ConvertToLines(sb);
        }

        private static StringBuilder ExtractAllBookmarks(string pdf)
        {
            StringBuilder sb = new StringBuilder();
            PdfReader reader = new PdfReader(pdf);
            IList<Dictionary<string, object>> bookmarksTree = SimpleBookmark.GetBookmark(reader);
            foreach (var node in bookmarksTree)
            {
                sb.AppendLine(PercorreBookmarks(node).ToString());
            }
            return RemoveAllBlankLines(sb);
        }

        private static StringBuilder RemoveAllBlankLines(StringBuilder sb)
        {
            return new StringBuilder().Append(Regex.Replace(sb.ToString(), @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline));
        }

        private static StringBuilder PercorreBookmarks(Dictionary<string, object> bookmark)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(bookmark["Title"].ToString());
            if (bookmark != null && bookmark.ContainsKey("Kids"))
            {
                IList<Dictionary<string, object>> children = (IList<Dictionary<string, object>>) bookmark["Kids"];
                foreach (var bm in children)
                {
                    sb.AppendLine(PercorreBookmarks(bm).ToString());
                }
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