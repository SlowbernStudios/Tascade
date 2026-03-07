using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
    private bool _autoMarkdownEnabled = true;

    [ObservableProperty]
    private bool _canUndo;

    [ObservableProperty]
    private bool _canRedo;

    [ObservableProperty]
    private string _currentTabTitle = string.Empty;

    public AutoCompleteService AutoCompleteService => _autoCompleteService;
    public VimModeService? VimModeService => _vimModeService;

    public Func<Task>? UndoRequested { get; set; }
    public Func<Task>? RedoRequested { get; set; }
    public Func<Task>? CutRequested { get; set; }
    public Func<Task>? CopyRequested { get; set; }
    public Func<Task>? PasteRequested { get; set; }
    public Func<Task>? FindRequested { get; set; }
    public Func<Task>? ReplaceRequested { get; set; }
    public Func<Task>? SelectAllRequested { get; set; }
    public Func<Task>? PrintRequested { get; set; }
    public Func<Task>? ShowSettingsRequested { get; set; }
    public Func<NotepadData, Task<bool>>? ConfirmCloseNotepadAsync { get; set; }
    public Func<Task<bool>>? ConfirmExitAsync { get; set; }
    public Func<string, string, Task<string?>>? ExportFilePathRequestedAsync { get; set; }

    public ObservableCollection<NotepadData> Notepads { get; }
    public ObservableCollection<TaskItem> FilteredTasks { get; }
    public ObservableCollection<string> RecentFiles { get; }

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
        RecentFiles = new ObservableCollection<string>(_settings.RecentFiles ?? Enumerable.Empty<string>());
        RecentFiles.CollectionChanged += OnRecentFilesCollectionChanged;

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
        AutoMarkdownEnabled = _settings.AutoMarkdownEnabled;

        UpdateCurrentTab();
        UpdateFilteredTasks();
        AttachContentSubscriptions();
    }

    public void InitializeFileOperations(Window window)
    {
        _fileOperationsService = new FileOperationsService(window);
        _fileOperationsService.RecentFilesUpdated += UpdateRecentFiles;

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
        AttachContentSubscriptions();
    }

    partial void OnCurrentViewModeChanged(ViewMode value) => MarkDirty();

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

    private void AttachContentSubscriptions()
    {
        foreach (var notepad in Notepads)
        {
            notepad.Content.PropertyChanged -= OnCurrentContentPropertyChanged;
            notepad.Content.PropertyChanged += OnCurrentContentPropertyChanged;
        }
    }

    private void OnCurrentContentPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender != CurrentNotepad.Content)
        {
            return;
        }

        if (e.PropertyName == nameof(RichTextContent.MarkdownContent)
            || e.PropertyName == nameof(RichTextContent.PlainText)
            || e.PropertyName == nameof(RichTextContent.HtmlContent))
        {
            MarkDirty();
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
        newNotepad.Content.PropertyChanged += OnCurrentContentPropertyChanged;
        CurrentNotepad = newNotepad;
    }

    [RelayCommand]
    private async Task CloseNotepad(NotepadData notepad)
    {
        if (Notepads.Count <= 1)
        {
            return;
        }

        if (ConfirmCloseNotepadAsync != null && !await ConfirmCloseNotepadAsync(notepad))
        {
            return;
        }

        var index = Notepads.IndexOf(notepad);
        Notepads.Remove(notepad);
        notepad.Content.PropertyChanged -= OnCurrentContentPropertyChanged;
        if (CurrentNotepad == notepad)
        {
            CurrentNotepad = Notepads[Math.Clamp(index - 1, 0, Notepads.Count - 1)];
        }

        SaveSettingsOnly();
    }

    [RelayCommand]
    private void SelectTab(NotepadData notepad) => CurrentNotepad = notepad;

    [RelayCommand]
    private void BeginEditTask(TaskItem task)
    {
        foreach (var item in CurrentNotepad.Tasks)
        {
            if (!ReferenceEquals(item, task))
            {
                item.IsEditing = false;
            }
        }

        task.IsEditing = true;
    }

    [RelayCommand]
    private void CommitTaskEdit(TaskItem task)
    {
        var updatedText = task.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(updatedText))
        {
            DeleteTask(task);
            return;
        }

        task.Text = updatedText;
        task.IsEditing = false;
        task.Updated = DateTime.Now;
        UpdateFilteredTasks();
        MarkDirty();
    }

    [RelayCommand]
    private void CancelTaskEdit(TaskItem task)
    {
        task.IsEditing = false;
        UpdateFilteredTasks();
    }

    [RelayCommand]
    private async Task Save()
    {
        if (_fileOperationsService == null)
        {
            CurrentNotepad.IsDirty = false;
            SaveSettingsOnly();
            return;
        }

        var path = CurrentNotepad.FilePath;
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
            CurrentNotepad.FilePath = path;
            CurrentNotepad.IsDirty = false;
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

        var existing = Notepads.FirstOrDefault(n => !string.IsNullOrWhiteSpace(n.FilePath)
            && string.Equals(n.FilePath, path, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            CurrentNotepad = existing;
            return;
        }

        var notepad = await _fileOperationsService.LoadNotepadAsync(path);
        if (notepad != null)
        {
            Notepads.Add(notepad);
            notepad.IsDirty = false;
            notepad.Content.PropertyChanged += OnCurrentContentPropertyChanged;
            CurrentNotepad = notepad;
            SaveSettingsOnly();
        }
    }

    [RelayCommand]
    private async Task OpenRecentFile(string? path)
    {
        if (_fileOperationsService == null || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return;
        }

        var existing = Notepads.FirstOrDefault(n => !string.IsNullOrWhiteSpace(n.FilePath)
            && string.Equals(n.FilePath, path, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            CurrentNotepad = existing;
            return;
        }

        var notepad = await _fileOperationsService.LoadNotepadAsync(path);
        if (notepad != null)
        {
            Notepads.Add(notepad);
            notepad.IsDirty = false;
            notepad.Content.PropertyChanged += OnCurrentContentPropertyChanged;
            CurrentNotepad = notepad;
            SaveSettingsOnly();
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
            CurrentNotepad.FilePath = path;
            CurrentNotepad.IsDirty = false;
            SaveSettingsOnly();
        }
    }

    [RelayCommand] private void NewFile() { AddNewNotepad(); }
    [RelayCommand] private async Task Print() { if (PrintRequested != null) await PrintRequested(); }
    [RelayCommand]
    private async Task Exit()
    {
        if (ConfirmExitAsync != null && !await ConfirmExitAsync())
        {
            return;
        }

        SaveSettingsOnly();
    }
    [RelayCommand] private async Task Undo() { if (UndoRequested != null) await UndoRequested(); }
    [RelayCommand] private async Task Redo() { if (RedoRequested != null) await RedoRequested(); }
    [RelayCommand] private async Task Cut() { if (CutRequested != null) await CutRequested(); }
    [RelayCommand] private async Task Copy() { if (CopyRequested != null) await CopyRequested(); }
    [RelayCommand] private async Task Paste() { if (PasteRequested != null) await PasteRequested(); }
    [RelayCommand] private async Task Find() { if (FindRequested != null) await FindRequested(); }
    [RelayCommand] private async Task Replace() { if (ReplaceRequested != null) await ReplaceRequested(); }
    [RelayCommand] private async Task SelectAll() { if (SelectAllRequested != null) await SelectAllRequested(); }

    [RelayCommand]
    private void InsertDateTime()
    {
        CurrentNotepad.Content.MarkdownContent += (CurrentNotepad.Content.MarkdownContent.Length > 0 ? Environment.NewLine : string.Empty)
            + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        MarkDirty();
    }

    [RelayCommand] private void ToggleWordWrap() { WordWrapEnabled = !WordWrapEnabled; MarkDirty(); }

    [RelayCommand] private void ToggleAutoMarkdown() { AutoMarkdownEnabled = !AutoMarkdownEnabled; MarkDirty(); }

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
    [RelayCommand] private async Task ShowSettings() { if (ShowSettingsRequested != null) await ShowSettingsRequested(); }

    [RelayCommand]
    private async Task ExportToText()
    {
        if (ExportFilePathRequestedAsync == null)
        {
            return;
        }

        var fileName = $"Notepad_{CurrentNotepad.Title}_{DateTime.Now:yyyy-MM-dd_HHmm}.txt";
        var filePath = await ExportFilePathRequestedAsync(fileName, "text");
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        _storageService.ExportToText(CurrentNotepad, filePath);
    }

    [RelayCommand]
    private async Task ExportToMarkdown()
    {
        if (ExportFilePathRequestedAsync == null)
        {
            return;
        }

        var fileName = $"Notepad_{CurrentNotepad.Title}_{DateTime.Now:yyyy-MM-dd_HHmm}.md";
        var filePath = await ExportFilePathRequestedAsync(fileName, "markdown");
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        await File.WriteAllTextAsync(filePath, CurrentNotepad.Content.MarkdownContent);
    }

    [RelayCommand]
    private async Task ExportToHtml()
    {
        if (ExportFilePathRequestedAsync == null)
        {
            return;
        }

        var fileName = $"Notepad_{CurrentNotepad.Title}_{DateTime.Now:yyyy-MM-dd_HHmm}.html";
        var filePath = await ExportFilePathRequestedAsync(fileName, "html");
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

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
        CurrentNotepad.IsDirty = true;
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
        _settings.AutoMarkdownEnabled = AutoMarkdownEnabled;
        _settings.RecentFiles = RecentFiles.ToList();

        _storageService.SaveSettings(_settings);

        if (_fileOperationsService != null)
        {
            var recentPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tascade", "recent_files.json");
            _fileOperationsService.SaveRecentFiles(recentPath);
        }
    }

    private void UpdateRecentFiles(System.Collections.Generic.List<string> files)
    {
        RecentFiles.Clear();
        foreach (var file in files)
        {
            RecentFiles.Add(file);
        }
    }

    private void OnRecentFilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _settings.RecentFiles = RecentFiles.ToList();
    }
}

