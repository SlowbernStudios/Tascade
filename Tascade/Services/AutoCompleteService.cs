using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tascade.Models;

namespace Tascade.Services
{
    public enum SuggestionType
    {
        Word,
        Command,
        FilePath,
        Snippet
    }

    public class SuggestionItem
    {
        public string Text { get; set; } = string.Empty;
        public string DisplayText { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public SuggestionType Type { get; set; }
    }

    public class AutoCompleteService
    {
        private readonly AutoCompleteSettings _settings;
        private readonly HashSet<string> _wordPool;

        private static readonly string[] Commands =
        {
            ":w", ":q", ":wq", ":q!", ":help"
        };

        public AutoCompleteService(AutoCompleteSettings settings)
        {
            _settings = settings;
            _wordPool = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public bool ShouldTriggerCompletion(string text, int cursorPosition)
        {
            if (!_settings.IsEnabled)
            {
                return false;
            }

            if (cursorPosition <= 0 || cursorPosition > text.Length)
            {
                return false;
            }

            var word = GetWordBeforeCursor(text, cursorPosition);
            if (word.Length >= _settings.MinWordLength)
            {
                return true;
            }

            return false;
        }

        public List<SuggestionItem> GetSuggestions(string seed)
        {
            if (!_settings.IsEnabled)
            {
                return new List<SuggestionItem>();
            }

            seed ??= string.Empty;

            var all = new List<SuggestionItem>();

            if (seed.Length < _settings.MinWordLength)
            {
                return new List<SuggestionItem>();
            }

            if (_settings.EnableSnippetCompletion)
            {
                all.AddRange(GetSnippetSuggestions(seed));
            }

            if (_settings.EnableWordCompletion)
            {
                all.AddRange(_wordPool
                    .Where(w => w.Length >= _settings.MinWordLength)
                    .Where(w => StartsWith(w, seed))
                    .OrderBy(w => w)
                    .Select(w => new SuggestionItem
                    {
                        Text = w,
                        DisplayText = w,
                        Description = "Word",
                        Type = SuggestionType.Word
                    }));
            }

            if (_settings.EnableCommandCompletion && seed.StartsWith(':'))
            {
                all.AddRange(Commands
                    .Where(c => StartsWith(c, seed))
                    .Select(c => new SuggestionItem
                    {
                        Text = c,
                        DisplayText = c,
                        Description = "Command",
                        Type = SuggestionType.Command
                    }));
            }

            if (_settings.EnableFilePathCompletion && (seed.Contains('/') || seed.Contains('\\') || seed.Contains('.')))
            {
                all.AddRange(GetFilePathSuggestions(seed));
            }

            return all
                .GroupBy(s => s.Text, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .Take(_settings.MaxSuggestions)
                .ToList();
        }

        public void UpdateWords(IEnumerable<string> words)
        {
            foreach (var word in words)
            {
                if (!string.IsNullOrWhiteSpace(word))
                {
                    _wordPool.Add(word);
                }
            }
        }

        private static string GetWordBeforeCursor(string text, int cursorPosition)
        {
            var start = cursorPosition - 1;
            while (start >= 0)
            {
                var c = text[start];
                if (!char.IsLetterOrDigit(c) && c != '_' && c != ':' && c != '/' && c != '\\' && c != '.')
                {
                    break;
                }

                start--;
            }

            return text.Substring(start + 1, cursorPosition - start - 1);
        }

        private bool StartsWith(string value, string seed)
        {
            var comparison = _settings.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            return value.StartsWith(seed, comparison);
        }

        private static IEnumerable<SuggestionItem> GetSnippetSuggestions(string seed)
        {
            var snippets = new[]
            {
                ("todo", "TODO: ", "Snippet"),
                ("fixme", "FIXME: ", "Snippet"),
                ("date", DateTime.Now.ToString("yyyy-MM-dd"), "Snippet"),
                ("note", "Note: ", "Snippet"),
                ("idea", "Idea: ", "Snippet"),
                ("next", "Next: ", "Snippet"),
                ("done", "Done: ", "Snippet")
            };

            return snippets
                .Where(s => s.Item1.StartsWith(seed, StringComparison.OrdinalIgnoreCase))
                .Select(s => new SuggestionItem
                {
                    Text = s.Item2,
                    DisplayText = s.Item1,
                    Description = s.Item3,
                    Type = SuggestionType.Snippet
                });
        }

        private static IEnumerable<SuggestionItem> GetFilePathSuggestions(string seed)
        {
            if (string.IsNullOrWhiteSpace(seed))
            {
                return Enumerable.Empty<SuggestionItem>();
            }

            try
            {
                var dir = Path.GetDirectoryName(seed);
                var prefix = Path.GetFileName(seed);

                if (string.IsNullOrWhiteSpace(dir))
                {
                    dir = Directory.GetCurrentDirectory();
                }

                if (!Directory.Exists(dir))
                {
                    return Enumerable.Empty<SuggestionItem>();
                }

                return Directory.EnumerateFileSystemEntries(dir)
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrEmpty(name) && name.StartsWith(prefix ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                    .Take(10)
                    .Select(name => new SuggestionItem
                    {
                        Text = Path.Combine(dir, name!),
                        DisplayText = name!,
                        Description = "Path",
                        Type = SuggestionType.FilePath
                    })
                    .ToList();
            }
            catch
            {
                return Enumerable.Empty<SuggestionItem>();
            }
        }
    }
}
