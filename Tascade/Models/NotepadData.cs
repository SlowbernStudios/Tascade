using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Tascade.Models
{
    public class NotepadData : INotifyPropertyChanged
    {
        private string _title = "Main";
        private bool _isCurrent;
        private string _text = string.Empty;
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
                }
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged(nameof(Text));
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
                }
            }
        }

        [JsonIgnore]
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

        public ObservableCollection<TaskItem> Tasks { get; set; } = new();
        public DateTime LastSaved { get; set; } = DateTime.Now;
        public int Version { get; set; } = 2;

        [JsonIgnore]
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

        [JsonPropertyName("FreeformNotes")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? LegacyFreeformNotes
        {
            get => null;
            set
            {
                if (string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(value))
                {
                    Text = value;
                }
            }
        }

        [JsonPropertyName("Content")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public LegacyContentData? LegacyContent
        {
            get => null;
            set
            {
                if (value == null || !string.IsNullOrEmpty(Text))
                {
                    return;
                }

                Text = value.PlainText
                    ?? value.MarkdownContent
                    ?? value.HtmlContent
                    ?? string.Empty;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class LegacyContentData
    {
        public string? MarkdownContent { get; set; }
        public string? HtmlContent { get; set; }
        public string? PlainText { get; set; }
    }
}
