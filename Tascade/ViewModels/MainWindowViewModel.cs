using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tascade.Models;
using Tascade.Services;

namespace Tascade.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private const int AutosaveDelayMs = 300;

    private readonly FileStorageService _storageService;
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private CancellationTokenSource? _autosaveCts;
    private FileOperationsService? _fileOperationsService;
    private AppSettings _settings;
    private bool _isInitializing;

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
    private bool _wordWrapEnabled = true;

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

    public ObservableCollection<NotepadData> Notepads { get; }
    public ObservableCollection<TaskItem> FilteredTasks { get; }
    public ObservableCollection<string> RecentFiles { get; }

    public MainWindowViewModel()
    {
        _isInitializing = true;
        _storageService = new FileStorageService();
        _settings = _storageService.LoadSettings();

        Notepads = new ObservableCollection<NotepadData>(_settings.Notepads);

        if (Notepads.Count == 0)
        {
            Notepads.Add(new NotepadData { Title = "Main" });
        }

        foreach (var notepad in Notepads)
        {
            SubscribeToNotepad(notepad);
        }

        Notepads.CollectionChanged += OnNotepadsCollectionChanged;

        FilteredTasks = new ObservableCollection<TaskItem>();
        RecentFiles = new ObservableCollection<string>(_settings.RecentFiles ?? Enumerable.Empty<string>());
        RecentFiles.CollectionChanged += OnRecentFilesCollectionChanged;

        var selectedIndex = Math.Clamp(_settings.SelectedIndex, 0, Notepads.Count - 1);
        _currentNotepad = Notepads[selectedIndex];
        _currentTabTitle = _currentNotepad.Title;

        ShowCompleted = _settings.ShowCompletedTasks;
        TasksWidth = _settings.TasksWidth;
        WordWrapEnabled = _settings.WordWrapEnabled;
        ZoomLevel = _settings.ZoomLevel;
        ShowStatusBar = _settings.ShowStatusBar;
        ShowTasksPanel = _settings.ShowTasksPanel;

        UpdateCurrentTab();
        UpdateFilteredTasks();
        _isInitializing = false;
    }

    public void InitializeFileOperations(Window window)
    {
        _fileOperationsService = new FileOperationsService(window);
        _fileOperationsService.RecentFilesUpdated += UpdateRecentFiles;

        var recentFilesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tascade", "recent_files.json");
        _fileOperationsService.LoadRecentFiles(recentFilesPath);
    }

    public async Task FlushPendingChangesAsync()
    {
        _autosaveCts?.Cancel();
        await PersistStateAsync(CancellationToken.None);
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
        }
    }

    partial void OnSearchTextChanged(string value) => UpdateFilteredTasks();
    partial void OnShowCompletedChanged(bool value)
    {
        UpdateFilteredTasks();
        PersistSettingsOnly();
    }

    partial void OnWordWrapEnabledChanged(bool value) => PersistSettingsOnly();
    partial void OnZoomLevelChanged(double value) => PersistSettingsOnly();
    partial void OnShowStatusBarChanged(bool value) => PersistSettingsOnly();
    partial void OnShowTasksPanelChanged(bool value) => PersistSettingsOnly();
    partial void OnTasksWidthChanged(float value) => PersistSettingsOnly();

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
    }

    [RelayCommand]
    private void ToggleTask(TaskItem task)
    {
        task.Done = !task.Done;
        task.Updated = DateTime.Now;
    }

    [RelayCommand]
    private void DeleteTask(TaskItem task)
    {
        CurrentNotepad.Tasks.Remove(task);
    }

    [RelayCommand]
    private void ClearCompletedTasks()
    {
        var completed = CurrentNotepad.Tasks.Where(t => t.Done).ToList();
        foreach (var task in completed)
        {
            CurrentNotepad.Tasks.Remove(task);
        }
    }

    [RelayCommand]
    private void AddNewNotepad()
    {
        var newNotepad = new NotepadData { Title = $"New Notepad {Notepads.Count + 1}" };
        Notepads.Add(newNotepad);
        CurrentNotepad = newNotepad;
    }

    [RelayCommand]
    private async Task CloseNotepad(NotepadData notepad)
    {
        if (Notepads.Count <= 1)
        {
            return;
        }

        await FlushPendingChangesAsync();

        var index = Notepads.IndexOf(notepad);
        Notepads.Remove(notepad);

        if (CurrentNotepad == notepad)
        {
            CurrentNotepad = Notepads[Math.Clamp(index - 1, 0, Notepads.Count - 1)];
        }

        PersistSettingsOnly();
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
        await FlushPendingChangesAsync();
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
            notepad.IsDirty = false;
            Notepads.Add(notepad);
            CurrentNotepad = notepad;
            await FlushPendingChangesAsync();
        }
    }

    [RelayCommand]
    private async Task OpenRecentFile(string? path)
    {
        if (_fileOperationsService == null || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (!File.Exists(path))
        {
            RecentFiles.Remove(path);
            PersistSettingsOnly();
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
            notepad.IsDirty = false;
            Notepads.Add(notepad);
            CurrentNotepad = notepad;
            await FlushPendingChangesAsync();
        }
    }

    [RelayCommand]
    private async Task SaveAs()
    {
        if (_fileOperationsService == null)
        {
            return;
        }

        var previousPath = CurrentNotepad.FilePath;
        var suggestedName = string.IsNullOrWhiteSpace(CurrentNotepad.FilePath)
            ? $"{CurrentNotepad.Title}.txt"
            : Path.GetFileName(CurrentNotepad.FilePath);

        var path = await _fileOperationsService.SaveFileDialogAsync(suggestedName);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        CurrentNotepad.FilePath = path;
        if (string.IsNullOrWhiteSpace(previousPath))
        {
            CurrentNotepad.Title = Path.GetFileNameWithoutExtension(path);
        }

        CurrentNotepad.IsDirty = true;
        await FlushPendingChangesAsync();
    }

    [RelayCommand] private void NewFile() => AddNewNotepad();
    [RelayCommand] private async Task Print() { if (PrintRequested != null) await PrintRequested(); }
    [RelayCommand] private async Task Exit() => await FlushPendingChangesAsync();
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
        CurrentNotepad.Text += (CurrentNotepad.Text.Length > 0 ? Environment.NewLine : string.Empty)
            + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    [RelayCommand]
    private void ToggleWordWrap() => WordWrapEnabled = !WordWrapEnabled;

    [RelayCommand]
    private void ZoomIn()
    {
        if (ZoomLevel < 2.0)
        {
            ZoomLevel += 0.1;
        }
    }

    [RelayCommand]
    private void ZoomOut()
    {
        if (ZoomLevel > 0.5)
        {
            ZoomLevel -= 0.1;
        }
    }

    [RelayCommand]
    private void ResetZoom() => ZoomLevel = 1.0;

    [RelayCommand]
    private void ToggleTasksPanel() => ShowTasksPanel = !ShowTasksPanel;

    [RelayCommand]
    private void ToggleStatusBar() => ShowStatusBar = !ShowStatusBar;

    [RelayCommand]
    private async Task ShowSettings()
    {
        if (ShowSettingsRequested != null)
        {
            await ShowSettingsRequested();
        }
    }

    private void SubscribeToNotepad(NotepadData notepad)
    {
        notepad.PropertyChanged += OnNotepadPropertyChanged;
        notepad.Tasks.CollectionChanged += OnTasksCollectionChanged;

        foreach (var task in notepad.Tasks)
        {
            task.PropertyChanged += OnTaskPropertyChanged;
        }
    }

    private void UnsubscribeFromNotepad(NotepadData notepad)
    {
        notepad.PropertyChanged -= OnNotepadPropertyChanged;
        notepad.Tasks.CollectionChanged -= OnTasksCollectionChanged;

        foreach (var task in notepad.Tasks)
        {
            task.PropertyChanged -= OnTaskPropertyChanged;
        }
    }

    private void OnNotepadsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (NotepadData notepad in e.NewItems)
            {
                SubscribeToNotepad(notepad);
                if (!_isInitializing)
                {
                    notepad.IsDirty = true;
                }
            }
        }

        if (e.OldItems != null)
        {
            foreach (NotepadData notepad in e.OldItems)
            {
                UnsubscribeFromNotepad(notepad);
            }
        }

        if (!_isInitializing)
        {
            ScheduleAutosave();
        }
    }

    private void OnNotepadPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not NotepadData notepad)
        {
            return;
        }

        if (ReferenceEquals(notepad, CurrentNotepad) && e.PropertyName == nameof(NotepadData.Title))
        {
            CurrentTabTitle = notepad.Title;
        }

        if (e.PropertyName is nameof(NotepadData.Text) or nameof(NotepadData.Title) or nameof(NotepadData.FilePath))
        {
            MarkDirty(notepad);
        }
    }

    private void OnTasksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (TaskItem task in e.NewItems)
            {
                task.PropertyChanged += OnTaskPropertyChanged;
            }
        }

        if (e.OldItems != null)
        {
            foreach (TaskItem task in e.OldItems)
            {
                task.PropertyChanged -= OnTaskPropertyChanged;
            }
        }

        UpdateFilteredTasks();
        var owner = FindTaskOwner(sender);
        if (owner != null)
        {
            MarkDirty(owner);
        }
    }

    private void OnTaskPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not TaskItem task)
        {
            return;
        }

        if (e.PropertyName is nameof(TaskItem.Done) or nameof(TaskItem.Text))
        {
            UpdateFilteredTasks();
            var owner = Notepads.FirstOrDefault(n => n.Tasks.Contains(task));
            if (owner != null)
            {
                task.Updated = DateTime.Now;
                MarkDirty(owner);
            }
        }
    }

    private NotepadData? FindTaskOwner(object? sender)
    {
        return Notepads.FirstOrDefault(n => ReferenceEquals(n.Tasks, sender));
    }

    private void MarkDirty(NotepadData notepad)
    {
        if (_isInitializing)
        {
            return;
        }

        notepad.IsDirty = true;
        ScheduleAutosave();
    }

    private void ScheduleAutosave()
    {
        if (_isInitializing)
        {
            return;
        }

        _autosaveCts?.Cancel();
        _autosaveCts = new CancellationTokenSource();
        var token = _autosaveCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(AutosaveDelayMs, token);
                await PersistStateAsync(token);
            }
            catch (OperationCanceledException)
            {
            }
        }, token);
    }

    private async Task PersistStateAsync(CancellationToken cancellationToken)
    {
        await _saveLock.WaitAsync(cancellationToken);
        try
        {
            _settings.Notepads = Notepads.ToList();
            _settings.SelectedIndex = Notepads.IndexOf(CurrentNotepad);
            _settings.ShowCompletedTasks = ShowCompleted;
            _settings.TasksWidth = TasksWidth;
            _settings.WordWrapEnabled = WordWrapEnabled;
            _settings.ZoomLevel = ZoomLevel;
            _settings.ShowStatusBar = ShowStatusBar;
            _settings.ShowTasksPanel = ShowTasksPanel;
            _settings.RecentFiles = RecentFiles.ToList();

            _storageService.SaveSettings(_settings);

            if (_fileOperationsService != null)
            {
                foreach (var notepad in Notepads.Where(n => n.IsDirty && !string.IsNullOrWhiteSpace(n.FilePath)))
                {
                    var saved = await _fileOperationsService.SaveTextAsync(notepad.FilePath!, notepad.Text);
                    if (saved)
                    {
                        notepad.IsDirty = false;
                        notepad.LastSaved = DateTime.Now;
                    }
                }

                var recentPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tascade", "recent_files.json");
                _fileOperationsService.SaveRecentFiles(recentPath);
            }

            foreach (var localOnlyNotepad in Notepads.Where(n => n.IsDirty && string.IsNullOrWhiteSpace(n.FilePath)))
            {
                localOnlyNotepad.IsDirty = false;
                localOnlyNotepad.LastSaved = DateTime.Now;
            }
        }
        finally
        {
            _saveLock.Release();
        }
    }

    private void PersistSettingsOnly()
    {
        if (_isInitializing)
        {
            return;
        }

        _settings.Notepads = Notepads.ToList();
        _settings.SelectedIndex = Notepads.IndexOf(CurrentNotepad);
        _settings.ShowCompletedTasks = ShowCompleted;
        _settings.TasksWidth = TasksWidth;
        _settings.WordWrapEnabled = WordWrapEnabled;
        _settings.ZoomLevel = ZoomLevel;
        _settings.ShowStatusBar = ShowStatusBar;
        _settings.ShowTasksPanel = ShowTasksPanel;
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
        if (_isInitializing)
        {
            return;
        }

        _settings.RecentFiles = RecentFiles.ToList();
        _storageService.SaveSettings(_settings);
    }
}
