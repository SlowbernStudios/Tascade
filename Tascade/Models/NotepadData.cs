using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Tascade.Services;

namespace Tascade.Models
{
    public class RichTextContent : INotifyPropertyChanged
    {
        private string _markdownContent = "";
        private string _htmlContent = "";
        private string _plainText = "";
        
        public string MarkdownContent 
        { 
            get => _markdownContent;
            set
            {
                if (_markdownContent != value)
                {
                    _markdownContent = value;
                    // Auto-generate HTML and plain text when Markdown changes
                    UpdateFromMarkdown();
                    OnPropertyChanged(nameof(MarkdownContent));
                    OnPropertyChanged(nameof(PreviewText));
                }
            }
        }
        
        public string HtmlContent 
        { 
            get => _htmlContent;
            set
            {
                if (_htmlContent != value)
                {
                    _htmlContent = value;
                    // Extract plain text when HTML changes
                    _plainText = System.Text.RegularExpressions.Regex.Replace(_htmlContent, "<[^>]*>", "");
                    OnPropertyChanged(nameof(HtmlContent));
                    OnPropertyChanged(nameof(PlainText));
                }
            }
        }
        
        public string PlainText 
        { 
            get => _plainText;
            set
            {
                if (_plainText != value)
                {
                    _plainText = value;
                    // Generate basic HTML and Markdown from plain text
                    _markdownContent = value;
                    var encoder = HtmlEncoder.Create(UnicodeRanges.All);
                    _htmlContent = $"<p>{encoder.Encode(value)}</p>";
                    OnPropertyChanged(nameof(PlainText));
                    OnPropertyChanged(nameof(MarkdownContent));
                    OnPropertyChanged(nameof(PreviewText));
                }
            }
        }
        
        public string PreviewText
        {
            get => MarkdownRenderer.RenderToPreviewText(_markdownContent);
        }
        
        public bool IsMarkdownMode { get; set; } = false;
        public DateTime LastModified { get; set; } = DateTime.Now;
        
        private void UpdateFromMarkdown()
        {
            // Use Markdig for proper Markdown to HTML conversion
            _htmlContent = MarkdownRenderer.RenderToHtml(_markdownContent);
            _plainText = MarkdownRenderer.RenderToPlainText(_markdownContent);
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class NotepadData : INotifyPropertyChanged
    {
        private string _title = "Main";
        private bool _isCurrent = false;
        private RichTextContent _content = new RichTextContent();
        private string? _filePath;
        private bool _isDirty;

        public string Title 
        { 
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged(nameof(Title));
                    IsDirty = true;
                }
            }
        }
        
        // Keep for backward compatibility
        public string FreeformNotes 
        { 
            get => Content.PlainText;
            set 
            {
                Content.PlainText = value;
                IsDirty = true;
            }
        }
        
        public RichTextContent Content 
        { 
            get => _content;
            set
            {
                if (_content != value)
                {
                    _content = value;
                    OnPropertyChanged(nameof(Content));
                    IsDirty = true;
                }
            }
        }

        public string? FilePath
        {
            get => _filePath;
            set
            {
                if (_filePath != value)
                {
                    _filePath = value;
                    OnPropertyChanged(nameof(FilePath));
                    IsDirty = true;
                }
            }
        }
        
        public bool IsDirty
        {
            get => _isDirty;
            set
            {
                if (_isDirty != value)
                {
                    _isDirty = value;
                    OnPropertyChanged(nameof(IsDirty));
                }
            }
        }
        
        public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        public DateTime LastSaved { get; set; } = DateTime.Now;
        public int Version { get; set; } = 1;

        public bool IsCurrent 
        { 
            get => _isCurrent;
            set
            {
                if (_isCurrent != value)
                {
                    _isCurrent = value;
                    OnPropertyChanged(nameof(IsCurrent));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
