using System;
using System.IO;
using System.Collections.Generic;
using WebSearchServer.Logging;

namespace WebSearchServer.Search
{
    public class FileSearcher
    {
        private readonly string _textFilesPath;
        private readonly Logger _logger = Logger.Instance;

        public FileSearcher(string textFilesPath)
        {
            _textFilesPath = textFilesPath;
        }

        public Dictionary<string, Dictionary<string, int>> Search(List<string> keywords)
        {
            var results = new Dictionary<string, Dictionary<string, int>>();

            string[] files = Directory.GetFiles(_textFilesPath, "*.txt");
            _logger.Info($"[FileSearcher] Pronađeno {files.Length} fajlova u {_textFilesPath}");

            foreach (string filePath in files)
            {
                string fileName = Path.GetFileName(filePath);
                string content;

                try
                {
                    content = File.ReadAllText(filePath).ToLower();
                }
                catch (IOException ex)
                {
                    _logger.Error($"[FileSearcher] Greška pri čitanju {fileName}: {ex.Message}");
                    continue;
                }

                var wordCounts = new Dictionary<string, int>();

                foreach (string keyword in keywords)
                {
                    string kw = keyword.ToLower().Trim();
                    if (string.IsNullOrEmpty(kw)) continue;

                    int count = CountOccurrences(content, kw);
                    wordCounts[kw] = count;
                }

                results[fileName] = wordCounts;
                _logger.Info($"[FileSearcher] {fileName} — obrađen");
            }

            return results;
        }

        private static int CountOccurrences(string text, string keyword)
        {
            int count = 0;
            int index = 0;

            while ((index = text.IndexOf(keyword, index, StringComparison.Ordinal)) != -1)
            {
                count++;
                index += keyword.Length;
            }

            return count;
        }
    }
}