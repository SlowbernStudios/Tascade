using Markdig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace Tascade.Services
{
    public class MarkdownRenderer
    {
        private static readonly MarkdownPipeline _pipeline;
        
        static MarkdownRenderer()
        {
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseAutoIdentifiers()
                .UseAutoLinks()
                .UseEmojiAndSmiley()
                .UseTaskLists()
                .UsePipeTables()
                .UseGridTables()
                .UseFootnotes()
                .UseCitations()
                .UseCustomContainers()
                .UseGenericAttributes()
                .UseListExtras()
                .UseEmphasisExtras()
                .UseFigures()
                .UseMediaLinks()
                .UseMathematics()
                .Build();
        }
        
        public static string RenderToHtml(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return "";
                
            var html = Markdown.ToHtml(markdown, _pipeline);
            
            // Wrap in HTML document with Tango Dark styling
            var styledHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{
            font-family: 'JetBrains Mono', 'Cascadia Mono', Consolas, 'Courier New', monospace;
            background-color: #2E3436;
            color: #EEEEEC;
            margin: 20px;
            line-height: 1.6;
        }}
        h1, h2, h3, h4, h5, h6 {{
            color: #EEEEEC;
            border-bottom: 1px solid #555753;
            padding-bottom: 5px;
        }}
        h1 {{ font-size: 2em; margin-top: 0; }}
        h2 {{ font-size: 1.5em; }}
        h3 {{ font-size: 1.2em; }}
        h4 {{ font-size: 1.1em; }}
        h5 {{ font-size: 1em; }}
        h6 {{ font-size: 0.9em; }}
        p {{ margin: 10px 0; }}
        a {{
            color: #4E9A06;
            text-decoration: none;
        }}
        a:hover {{
            color: #6BA016;
            text-decoration: underline;
        }}
        code {{
            background-color: #555753;
            color: #EEEEEC;
            padding: 2px 4px;
            border-radius: 3px;
            font-family: 'JetBrains Mono', 'Cascadia Mono', Consolas, 'Courier New', monospace;
            font-size: 0.9em;
        }}
        pre {{
            background-color: #555753;
            color: #EEEEEC;
            padding: 15px;
            border-radius: 5px;
            overflow-x: auto;
            border: 1px solid #204A87;
        }}
        pre code {{
            background-color: transparent;
            padding: 0;
            border-radius: 0;
        }}
        blockquote {{
            border-left: 4px solid #4E9A06;
            margin: 20px 0;
            padding-left: 20px;
            color: #BABDB6;
            font-style: italic;
        }}
        table {{
            border-collapse: collapse;
            width: 100%;
            margin: 20px 0;
        }}
        th, td {{
            border: 1px solid #555753;
            padding: 8px 12px;
            text-align: left;
        }}
        th {{
            background-color: #4A4A4A;
            font-weight: bold;
        }}
        tr:nth-child(even) {{
            background-color: #3A3A3A;
        }}
        ul, ol {{
            margin: 10px 0;
            padding-left: 30px;
        }}
        li {{
            margin: 5px 0;
        }}
        hr {{
            border: none;
            height: 1px;
            background-color: #555753;
            margin: 30px 0;
        }}
        .task-list-item {{
            list-style: none;
        }}
        .task-list-item input {{
            margin-right: 10px;
        }}
        img {{
            max-width: 100%;
            height: auto;
            border-radius: 5px;
            border: 1px solid #555753;
        }}
    </style>
</head>
<body>
    {html}
</body>
</html>";
            
            return styledHtml;
        }
        
        public static string RenderToPreviewText(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return "";
                
            // Convert Markdown to a formatted text representation for preview
            var lines = markdown.Split('\n');
            var previewLines = new List<string>();
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                if (string.IsNullOrEmpty(trimmed))
                {
                    previewLines.Add("");
                    continue;
                }
                
                // Handle headers
                if (trimmed.StartsWith("#"))
                {
                    var level = trimmed.TakeWhile(c => c == '#').Count();
                    var text = trimmed.Substring(level).Trim();
                    var prefix = new string('=', Math.Max(1, 7 - level));
                    previewLines.Add($"{prefix} {text.ToUpper()}");
                }
                // Handle lists
                else if (trimmed.StartsWith("*") || trimmed.StartsWith("-") || trimmed.StartsWith("+"))
                {
                    var text = trimmed.Substring(1).Trim();
                    previewLines.Add($"• {text}");
                }
                else if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d+\."))
                {
                    var text = System.Text.RegularExpressions.Regex.Replace(trimmed, @"^\d+\.\s*", "");
                    previewLines.Add($"• {text}");
                }
                // Handle blockquotes
                else if (trimmed.StartsWith(">"))
                {
                    var text = trimmed.Substring(1).Trim();
                    previewLines.Add($"❝ {text}");
                }
                // Handle code blocks
                else if (trimmed.StartsWith("```"))
                {
                    previewLines.Add($"┌─ CODE BLOCK ─┐");
                }
                // Handle horizontal rules
                else if (trimmed == "---" || trimmed == "***")
                {
                    previewLines.Add(new string('─', 30));
                }
                // Handle emphasis
                else
                {
                    var processed = trimmed;
                    processed = System.Text.RegularExpressions.Regex.Replace(processed, @"\*\*(.*?)\*\*", "$1"); // Bold
                    processed = System.Text.RegularExpressions.Regex.Replace(processed, @"\*(.*?)\*", "$1"); // Italic
                    processed = System.Text.RegularExpressions.Regex.Replace(processed, @"`(.*?)`", "$1"); // Inline code
                    processed = System.Text.RegularExpressions.Regex.Replace(processed, @"~~(.*?)~~", "$1"); // Strikethrough
                    previewLines.Add(processed);
                }
            }
            
            return string.Join("\n", previewLines);
        }
        
        public static string RenderToPlainText(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return "";
                
            var html = RenderToHtml(markdown);
            
            // Simple HTML tag removal for plain text display
            var plainText = System.Text.RegularExpressions.Regex.Replace(html, @"<[^>]*>", "");
            
            // Clean up extra whitespace
            plainText = System.Text.RegularExpressions.Regex.Replace(plainText, @"\s+", " ");
            plainText = plainText.Replace("&lt;", "<");
            plainText = plainText.Replace("&gt;", ">");
            plainText = plainText.Replace("&amp;", "&");
            plainText = plainText.Replace("&quot;", "\"");
            plainText = plainText.Replace("&#39;", "'");
            
            return plainText.Trim();
        }
        
        public static string ExtractPlainText(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return "";
                
            // Basic markdown to plain text conversion
            var plain = markdown;
            
            // Remove markdown syntax
            plain = System.Text.RegularExpressions.Regex.Replace(plain, @"\*\*(.*?)\*\*", "$1"); // Bold
            plain = System.Text.RegularExpressions.Regex.Replace(plain, @"\*(.*?)\*", "$1"); // Italic
            plain = System.Text.RegularExpressions.Regex.Replace(plain, @"`(.*?)`", "$1"); // Inline code
            plain = System.Text.RegularExpressions.Regex.Replace(plain, @"^#{1,6}\s*", "", System.Text.RegularExpressions.RegexOptions.Multiline); // Headers
            plain = System.Text.RegularExpressions.Regex.Replace(plain, @"^\s*[-*+]\s*", "• ", System.Text.RegularExpressions.RegexOptions.Multiline); // Lists
            plain = System.Text.RegularExpressions.Regex.Replace(plain, @"^\s*\d+\.\s*", "• ", System.Text.RegularExpressions.RegexOptions.Multiline); // Numbered lists
            plain = System.Text.RegularExpressions.Regex.Replace(plain, @"^\s*>\s*", "", System.Text.RegularExpressions.RegexOptions.Multiline); // Blockquotes
            plain = System.Text.RegularExpressions.Regex.Replace(plain, @"^\s*\[\s*\]\s*", "☐ ", System.Text.RegularExpressions.RegexOptions.Multiline); // Task lists
            plain = System.Text.RegularExpressions.Regex.Replace(plain, @"^\s*\[x\]\s*", "☑ ", System.Text.RegularExpressions.RegexOptions.Multiline); // Completed task lists
            plain = System.Text.RegularExpressions.Regex.Replace(plain, @"^\s*---+\s*$", "", System.Text.RegularExpressions.RegexOptions.Multiline); // Horizontal rules
            plain = System.Text.RegularExpressions.Regex.Replace(plain, @"^\s*\*\*\*+\s*$", "", System.Text.RegularExpressions.RegexOptions.Multiline); // Horizontal rules
            
            return plain.Trim();
        }
    }
}

