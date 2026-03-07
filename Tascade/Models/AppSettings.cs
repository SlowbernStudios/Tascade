using System.Collections.Generic;

namespace Tascade.Models
{
    public enum ViewMode
    {
        Plain,
        Markdown,
        Split
    }

    public class AutoCompleteSettings
    {
        public bool IsEnabled { get; set; } = true;
        public bool EnableFilePathCompletion { get; set; } = true;
        public bool EnableCommandCompletion { get; set; } = true;
        public bool EnableSnippetCompletion { get; set; } = true;
        public bool EnableWordCompletion { get; set; } = true;
        public int MaxSuggestions { get; set; } = 10;
        public int MinWordLength { get; set; } = 2;
        public List<char> TriggerCharacters { get; set; } = new() { ':', '/', '\\', '.' };
        public bool ShowDescriptions { get; set; } = true;
        public bool CaseSensitive { get; set; } = false;
    }

    public class AppSettings
    {
        public List<NotepadData> Notepads { get; set; } = new List<NotepadData>();
        public int SelectedIndex { get; set; } = 0;
        public float TasksWidth { get; set; } = 300f;
        public bool AutoSave { get; set; } = true;
        public bool ShowCompletedTasks { get; set; } = true;
        public bool VimModeEnabled { get; set; } = false;
        public AutoCompleteSettings AutoCompleteSettings { get; set; } = new AutoCompleteSettings();
        
        // New toolbar-related settings
        public bool WordWrapEnabled { get; set; } = true;
        public ViewMode CurrentViewMode { get; set; } = ViewMode.Markdown;
        public double ZoomLevel { get; set; } = 1.0;
        public bool ShowStatusBar { get; set; } = false;
        public bool ShowTasksPanel { get; set; } = true;
        public bool AutoMarkdownEnabled { get; set; } = true;
        public List<string> RecentFiles { get; set; } = new List<string>();
    }
}
