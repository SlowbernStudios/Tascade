using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NotepadApp.Models
{
    public class NotepadData : INotifyPropertyChanged
    {
        private string _title = "Main";
        private bool _isCurrent = false;

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
        
        public string FreeformNotes { get; set; } = string.Empty;
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
