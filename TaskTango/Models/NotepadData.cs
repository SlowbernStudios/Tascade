using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace NotepadApp.Models
{
    public class RichTextContent
    {
        public string HtmlContent { get; set; } = "";
        public string PlainText { get; set; } = "";
        public DateTime LastModified { get; set; } = DateTime.Now;
    }

    public class NotepadData : INotifyPropertyChanged
    {
        private string _title = "Main";
        private bool _isCurrent = false;
        private RichTextContent _content = new RichTextContent();

        public string Title 
        { 
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged(nameof(Title));
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
                var encoder = HtmlEncoder.Create(UnicodeRanges.All);
                Content.HtmlContent = $"<p>{encoder.Encode(value)}</p>";
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
