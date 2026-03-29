using System.Collections.Generic;

namespace Tascade.Models
{
    public class AppSettings
    {
        public List<NotepadData> Notepads { get; set; } = new();
        public int SelectedIndex { get; set; }
        public float TasksWidth { get; set; } = 300f;
        public bool ShowCompletedTasks { get; set; } = true;
        public bool WordWrapEnabled { get; set; } = true;
        public double ZoomLevel { get; set; } = 1.0;
        public bool ShowStatusBar { get; set; }
        public bool ShowTasksPanel { get; set; } = true;
        public List<string> RecentFiles { get; set; } = new();
    }
}
