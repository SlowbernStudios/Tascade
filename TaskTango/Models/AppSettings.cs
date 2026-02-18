using System.Collections.Generic;

namespace NotepadApp.Models
{
    public class AppSettings
    {
        public List<NotepadData> Notepads { get; set; } = new List<NotepadData>();
        public int SelectedIndex { get; set; } = 0;
        public float TasksWidth { get; set; } = 300f;
        public bool AutoSave { get; set; } = true;
        public bool ShowCompletedTasks { get; set; } = true;
        public bool VimModeEnabled { get; set; } = false;
    }
}
