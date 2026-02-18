using System;
using System.Collections.Generic;

namespace NotepadApp.Models
{
    public class NotepadData
    {
        public string Title { get; set; } = "Main";
        public string FreeformNotes { get; set; } = string.Empty;
        public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        public DateTime LastSaved { get; set; } = DateTime.Now;
        public int Version { get; set; } = 1;
    }
}
