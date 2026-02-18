using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NotepadApp.Models;
using NotepadApp.Services;

namespace NotepadApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly FileStorageService _storageService;
    private AppSettings _settings;
    
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
    
    public ObservableCollection<NotepadData> Notepads { get; }
    public ObservableCollection<TaskItem> FilteredTasks { get; }
    
    public MainWindowViewModel()
    {
        _storageService = new FileStorageService();
        _settings = _storageService.LoadSettings();
        
        Notepads = new ObservableCollection<NotepadData>(_settings.Notepads);
        FilteredTasks = new ObservableCollection<TaskItem>();
        
        CurrentNotepad = Notepads[_settings.SelectedIndex];
        ShowCompleted = _settings.ShowCompletedTasks;
        TasksWidth = _settings.TasksWidth;
        AutoSave = _settings.AutoSave;
        
        UpdateFilteredTasks();
    }
    
    partial void OnCurrentNotepadChanged(NotepadData value)
    {
        UpdateFilteredTasks();
    }
    
    partial void OnSearchTextChanged(string value)
    {
        UpdateFilteredTasks();
    }
    
    partial void OnShowCompletedChanged(bool value)
    {
        UpdateFilteredTasks();
    }
    
    private void UpdateFilteredTasks()
    {
        FilteredTasks.Clear();
        
        if (CurrentNotepad?.Tasks == null) return;
        
        var filtered = CurrentNotepad.Tasks
            .Where(t => ShowCompleted || !t.Done)
            .Where(t => string.IsNullOrEmpty(SearchText) || 
                       t.Text.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.Done)
            .ThenBy(t => t.Created);
            
        foreach (var task in filtered)
        {
            FilteredTasks.Add(task);
        }
    }
    
    [RelayCommand]
    private void AddTask()
    {
        if (string.IsNullOrWhiteSpace(NewTaskText)) return;
        
        var task = new TaskItem 
        { 
            Text = NewTaskText.Trim(),
            Created = DateTime.Now,
            Updated = DateTime.Now
        };
        
        CurrentNotepad.Tasks.Add(task);
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
    private void SortTasks()
    {
        CurrentNotepad.Tasks = CurrentNotepad.Tasks
            .OrderBy(t => t.Done)
            .ThenBy(t => t.Created)
            .ToList();
        UpdateFilteredTasks();
        MarkDirty();
    }
    
    [RelayCommand]
    private void AddNewNotepad()
    {
        var newNotepad = new NotepadData 
        { 
            Title = $"New Notepad {Notepads.Count + 1}" 
        };
        Notepads.Add(newNotepad);
        CurrentNotepad = newNotepad;
        MarkDirty();
    }
    
    [RelayCommand]
    private void Save()
    {
        _settings.Notepads = Notepads.ToList();
        _settings.SelectedIndex = Notepads.IndexOf(CurrentNotepad);
        _settings.ShowCompletedTasks = ShowCompleted;
        _settings.TasksWidth = TasksWidth;
        _settings.AutoSave = AutoSave;
        
        _storageService.SaveSettings(_settings);
    }
    
    [RelayCommand]
    private async Task ExportToText()
    {
        // For now, use a simple export to the current directory
        // TODO: Implement proper file dialog when we have access to TopLevel
        var fileName = $"Notepad_{CurrentNotepad.Title}_{DateTime.Now:yyyy-MM-dd_HHmm}.txt";
        var filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
        
        try
        {
            _storageService.ExportToText(CurrentNotepad, filePath);
        }
        catch (Exception ex)
        {
            // TODO: Show error dialog
            System.Diagnostics.Debug.WriteLine($"Export failed: {ex.Message}");
        }
    }
    
    private void MarkDirty()
    {
        CurrentNotepad.LastSaved = DateTime.Now;
        if (AutoSave)
        {
            Save();
        }
    }
}
