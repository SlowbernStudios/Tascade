using System;

namespace NotepadApp.Models
{
    public class TaskItem
    {
        public bool Done { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime Updated { get; set; } = DateTime.Now;
    }
}
