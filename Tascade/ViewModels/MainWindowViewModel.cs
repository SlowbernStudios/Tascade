using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tascade.Models;
using Tascade.Services;

namespace Tascade.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly FileStorageService _storageService;
    private FileOperationsService? _fileOperationsService;
    private AppSettings _settings;
    private readonly AutoCompleteService _autoCompleteService;
    private VimModeService? _vimModeService;
    private string? _currentFilePath;

    [ObservableProperty]
    private NotepadData _currentNotepad;

    [ObservableProperty]
    private string _newTaskText = string.Empty;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _showCompleted = true;

    [ObservableProperty]
    private float _tasksWidth = 300f;

    [ObservableProperty]
    private bool _autoSave = true;

    [ObservableProperty]
    private bool _wordWrapEnabled = true;

    [ObservableProperty]
    private ViewMode _currentViewMode = ViewMode.Markdown;

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private bool _showStatusBar;

    [ObservableProperty]
    private bool _showTasksPanel = true;

    [ObservableProperty]
    private bool _canUndo;

    [ObservableProperty]
    private bool _canRedo;

    [ObservableProperty]
    private string _currentTabTitle = string.Empty;

    public AutoCompleteService AutoCompleteService => _autoCompleteService;
    public VimModeService? VimModeService => _vimModeService;

    public ObservableCollection<NotepadData> Notepads { get; }
    public ObservableCollection<TaskItem> FilteredTasks { get; }

    public MainWindowViewModel()
    {
        _storageService = new FileStorageService();
        _settings = _storageService.LoadSettings();
        _autoCompleteService = new AutoCompleteService(_settings.AutoCompleteSettings);

        Notepads = new ObservableCollection<NotepadData>(_settings.Notepads);
        if (Notepads.Count == 0)
        {
            Notepads.Add(new NotepadData { Title = "Main" });
        }

        FilteredTasks = new ObservableCollection<TaskItem>();

        var selectedIndex = Math.Clamp(_settings.SelectedIndex, 0, Notepads.Count - 1);
        _currentNotepad = Notepads[selectedIndex];
        _currentTabTitle = _currentNotepad.Title;

        ShowCompleted = _settings.ShowCompletedTasks;
        TasksWidth = _settings.TasksWidth;
        AutoSave = _settings.AutoSave;
        WordWrapEnabled = _settings.WordWrapEnabled;
        CurrentViewMode = _settings.CurrentViewMode;
        ZoomLevel = _settings.ZoomLevel;
        ShowStatusBar = _settings.ShowStatusBar;
        ShowTasksPanel = _settings.ShowTasksPanel;

        UpdateCurrentTab();
        UpdateFilteredTasks();
    }

    public void InitializeFileOperations(Window window)
    {
        _fileOperationsService = new FileOperationsService(window);
        _fileOperationsService.FileOpened += path => _currentFilePath = path;
        _fileOperationsService.FileSaved += path => _currentFilePath = path;
        _fileOperationsService.RecentFilesUpdated += files => _settings.RecentFiles = files;

        var recentFilesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tascade", "recent_files.json");
        _fileOperationsService.LoadRecentFiles(recentFilesPath);
    }

    public void SetVimModeService(TextBox textBox)
    {
        if (_settings.VimModeEnabled)
        {
            _vimModeService = new VimModeService(textBox);
            _vimModeService.AutoCompleteService = _autoCompleteService;
            _vimModeService.CommandEntered += HandleVimCommand;
        }
    }

    partial void OnCurrentNotepadChanged(NotepadData value)
    {
        CurrentTabTitle = value.Title;
        UpdateCurrentTab();
        UpdateFilteredTasks();
    }

    partial void OnCurrentTabTitleChanged(string value)
    {
        if (CurrentNotepad == null)
        {
            return;
        }

        var normalized = string.IsNullOrWhiteSpace(value) ? "Untitled" : value.Trim();
        if (!string.Equals(value, normalized, StringComparison.Ordinal))
        {
            CurrentTabTitle = normalized;
            return;
        }

        if (!string.Equals(CurrentNotepad.Title, normalized, StringComparison.Ordinal))
        {
            CurrentNotepad.Title = normalized;
            MarkDirty();
        }
    }

    partial void OnSearchTextChanged(string value) => UpdateFilteredTasks();
    partial void OnShowCompletedChanged(bool value) => UpdateFilteredTasks();

    private void UpdateFilteredTasks()
    {
        FilteredTasks.Clear();
        if (CurrentNotepad?.Tasks == null)
        {
            return;
        }

        var filtered = CurrentNotepad.Tasks
            .Where(t => ShowCompleted || !t.Done)
            .Where(t => string.IsNullOrEmpty(SearchText) || t.Text.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.Done)
            .ThenBy(t => t.Created);

        foreach (var task in filtered)
        {
            FilteredTasks.Add(task);
        }
    }

    private void UpdateCurrentTab()
    {
        foreach (var notepad in Notepads)
        {
            notepad.IsCurrent = notepad == CurrentNotepad;
        }
    }

    [RelayCommand]
    private void AddTask()
    {
        if (string.IsNullOrWhiteSpace(NewTaskText))
        {
            return;
        }

        CurrentNotepad.Tasks.Add(new TaskItem
        {
            Text = NewTaskText.Trim(),
            Created = DateTime.Now,
            Updated = DateTime.Now
        });

        NewTaskText = string.Empty;
        UpdateFilteredTasks();
        MarkDirty();
    }

    [RelayCommand]
    private void ToggleTask(TaskItem task)
    {
        task.Done = !task.Done;
        task.Updated = DateTime.Now;
        UpdateFilteredTasks();
        MarkDirty();
    }

    [RelayCommand]
    private void DeleteTask(TaskItem task)
    {
        CurrentNotepad.Tasks.Remove(task);
        UpdateFilteredTasks();
        MarkDirty();
    }

    [RelayCommand]
    private void ClearCompletedTasks()
    {
        CurrentNotepad.Tasks.RemoveAll(t => t.Done);
        UpdateFilteredTasks();
        MarkDirty();
    }

    [RelayCommand]
    private void AddNewNotepad()
    {
        var newNotepad = new NotepadData { Title = $"New Notepad {Notepads.Count + 1}", Content = new RichTextContent() };
        Notepads.Add(newNotepad);
        CurrentNotepad = newNotepad;
        MarkDirty();
    }

    [RelayCommand]
    private void CloseNotepad(NotepadData notepad)
    {
        if (Notepads.Count <= 1)
        {
            return;
        }

        var index = Notepads.IndexOf(notepad);
        Notepads.Remove(notepad);
        if (CurrentNotepad == notepad)
        {
            CurrentNotepad = Notepads[Math.Clamp(index - 1, 0, Notepads.Count - 1)];
        }

        MarkDirty();
    }

    [RelayCommand]
    private void SelectTab(NotepadData notepad) => CurrentNotepad = notepad;

    [RelayCommand]
    private async Task Save()
    {
        if (_fileOperationsService == null)
        {
            SaveSettingsOnly();
            return;
        }

        var path = _currentFilePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            path = await _fileOperationsService.SaveFileDialogAsync($"{CurrentNotepad.Title}.md");
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }
        }

        if (await _fileOperationsService.SaveNotepadAsync(path, CurrentNotepad))
        {
            _currentFilePath = path;
            SaveSettingsOnly();
        }
    }

    [RelayCommand]
    private async Task OpenFile()
    {
        if (_fileOperationsService == null)
        {
            return;
        }

        var path = await _fileOperationsService.OpenFileDialogAsync();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var notepad = await _fileOperationsService.LoadNotepadAsync(path);
        if (notepad != null)
        {
            Notepads.Add(notepad);
            CurrentNotepad = notepad;
            _currentFilePath = path;
            MarkDirty();
        }
    }

    [RelayCommand]
    private async Task SaveAs()
    {
        if (_fileOperationsService == null)
        {
            return;
        }

        var path = await _fileOperationsService.SaveFileDialogAsync($"{CurrentNotepad.Title}.md");
        if (!string.IsNullOrWhiteSpace(path) && await _fileOperationsService.SaveNotepadAsync(path, CurrentNotepad))
        {
            _currentFilePath = path;
            MarkDirty();
        }
    }

    [RelayCommand] private void NewFile() { _currentFilePath = null; AddNewNotepad(); }
    [RelayCommand] private void Print() { }
    [RelayCommand] private void Exit() => SaveSettingsOnly();
    [RelayCommand] private void Undo() { }
    [RelayCommand] private void Redo() { }
    [RelayCommand] private void Cut() { }
    [RelayCommand] private void Copy() { }
    [RelayCommand] private void Paste() { }
    [RelayCommand] private void Find() { }
    [RelayCommand] private void Replace() { }
    [RelayCommand] private void SelectAll() { }

    [RelayCommand]
    private void InsertDateTime()
    {
        CurrentNotepad.Content.MarkdownContent += (CurrentNotepad.Content.MarkdownContent.Length > 0 ? Environment.NewLine : string.Empty)
            + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        MarkDirty();
    }

    [RelayCommand] private void ToggleWordWrap() { WordWrapEnabled = !WordWrapEnabled; MarkDirty(); }

    [RelayCommand]
    private void SetViewMode(string mode)
    {
        if (Enum.TryParse<ViewMode>(mode, true, out var parsed))
        {
            CurrentViewMode = parsed;
            MarkDirty();
        }
    }

    [RelayCommand] private void ZoomIn() { if (ZoomLevel < 2.0) { ZoomLevel += 0.1; MarkDirty(); } }
    [RelayCommand] private void ZoomOut() { if (ZoomLevel > 0.5) { ZoomLevel -= 0.1; MarkDirty(); } }
    [RelayCommand] private void ResetZoom() { ZoomLevel = 1.0; MarkDirty(); }
    [RelayCommand] private void ToggleTasksPanel() { ShowTasksPanel = !ShowTasksPanel; MarkDirty(); }
    [RelayCommand] private void ToggleStatusBar() { ShowStatusBar = !ShowStatusBar; MarkDirty(); }
    [RelayCommand] private void ShowSettings() { }

    [RelayCommand]
    private void ExportToText()
    {
        var fileName = $"Notepad_{CurrentNotepad.Title}_{DateTime.Now:yyyy-MM-dd_HHmm}.txt";
        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
        _storageService.ExportToText(CurrentNotepad, filePath);
    }

    [RelayCommand]
    private async Task ExportToMarkdown()
    {
        var fileName = $"Notepad_{CurrentNotepad.Title}_{DateTime.Now:yyyy-MM-dd_HHmm}.md";
        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
        await File.WriteAllTextAsync(filePath, CurrentNotepad.Content.MarkdownContent);
    }

    [RelayCommand]
    private async Task ExportToHtml()
    {
        var fileName = $"Notepad_{CurrentNotepad.Title}_{DateTime.Now:yyyy-MM-dd_HHmm}.html";
        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
        await File.WriteAllTextAsync(filePath, CurrentNotepad.Content.HtmlContent);
    }

    private void HandleVimCommand(string command)
    {
        if (string.Equals(command, "save", StringComparison.OrdinalIgnoreCase))
        {
            _ = Save();
        }
    }

    private void MarkDirty()
    {
        CurrentNotepad.LastSaved = DateTime.Now;
        if (AutoSave)
        {
            SaveSettingsOnly();
        }
    }

    private void SaveSettingsOnly()
    {
        _settings.Notepads = Notepads.ToList();
        _settings.SelectedIndex = Notepads.IndexOf(CurrentNotepad);
        _settings.ShowCompletedTasks = ShowCompleted;
        _settings.TasksWidth = TasksWidth;
        _settings.AutoSave = AutoSave;
        _settings.WordWrapEnabled = WordWrapEnabled;
        _settings.CurrentViewMode = CurrentViewMode;
        _settings.ZoomLevel = ZoomLevel;
        _settings.ShowStatusBar = ShowStatusBar;
        _settings.ShowTasksPanel = ShowTasksPanel;

        _storageService.SaveSettings(_settings);

        if (_fileOperationsService != null)
        {
            var recentPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tascade", "recent_files.json");
            _fileOperationsService.SaveRecentFiles(recentPath);
        }
    }
}

