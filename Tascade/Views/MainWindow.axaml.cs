using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Input;
using Tascade.Controls;
using Tascade.Models;
using Tascade.ViewModels;

namespace Tascade.Views;

public partial class MainWindow : Window
{
    private MarkdownEditor? _markdownEditor;
    private TextBox? _addTaskTextBox;
    private MainWindowViewModel? _subscribedViewModel;
    private Grid? _contentGrid;
    private GridSplitter? _taskGridSplitter;
    private Border? _tasksPanelBorder;
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();

        _markdownEditor = this.FindControl<MarkdownEditor>("MarkdownEditor");
        _addTaskTextBox = this.FindControl<TextBox>("AddTaskTextBox");
        _contentGrid = this.FindControl<Grid>("ContentGrid");
        _taskGridSplitter = this.FindControl<GridSplitter>("TaskGridSplitter");
        _tasksPanelBorder = this.FindControl<Border>("TasksPanelBorder");
        if (_addTaskTextBox != null)
        {
            _addTaskTextBox.KeyDown += OnAddTaskTextBoxKeyDown;
        }

        if (_markdownEditor != null)
        {
            _markdownEditor.HistoryStateChanged += OnEditorHistoryStateChanged;
        }

        DataContextChanged += OnDataContextChanged;
        Opened += OnWindowOpened;
        Closing += OnWindowClosing;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.InitializeFileOperations(this);
        }
    }

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        e.Cancel = true;
        if (await ConfirmExitAsync())
        {
            _allowClose = true;
            Close();
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel && _markdownEditor != null)
        {
            if (!ReferenceEquals(_subscribedViewModel, viewModel))
            {
                _subscribedViewModel = viewModel;
                WireViewModelCallbacks(viewModel);
                viewModel.PropertyChanged += (s, args) =>
                {
                    if (_markdownEditor == null)
                    {
                        return;
                    }

                    if (args.PropertyName == nameof(viewModel.CurrentNotepad))
                    {
                        _markdownEditor.Content = viewModel.CurrentNotepad.Content;
                    }

                    if (args.PropertyName == nameof(viewModel.AutoMarkdownEnabled))
                    {
                        _markdownEditor.AutoMarkdownEnabled = viewModel.AutoMarkdownEnabled;
                    }

                    if (args.PropertyName == nameof(viewModel.CurrentViewMode))
                    {
                        _markdownEditor.ViewMode = viewModel.CurrentViewMode;
                    }

                    if (args.PropertyName == nameof(viewModel.AutoCompleteService))
                    {
                        _markdownEditor.AutoCompleteService = viewModel.AutoCompleteService;
                    }

                    if (args.PropertyName == nameof(viewModel.WordWrapEnabled))
                    {
                        _markdownEditor.WordWrapEnabled = viewModel.WordWrapEnabled;
                    }

                    if (args.PropertyName == nameof(viewModel.ZoomLevel))
                    {
                        _markdownEditor.ZoomLevel = viewModel.ZoomLevel;
                    }

                    if (args.PropertyName == nameof(viewModel.ShowTasksPanel))
                    {
                        ApplyTasksPanelVisibility(viewModel.ShowTasksPanel, viewModel.TasksWidth);
                    }
                };
            }

            _markdownEditor.Content = viewModel.CurrentNotepad.Content;
            _markdownEditor.AutoMarkdownEnabled = viewModel.AutoMarkdownEnabled;
            _markdownEditor.ViewMode = viewModel.CurrentViewMode;
            _markdownEditor.AutoCompleteService = viewModel.AutoCompleteService;
            _markdownEditor.WordWrapEnabled = viewModel.WordWrapEnabled;
            _markdownEditor.ZoomLevel = viewModel.ZoomLevel;
            viewModel.CanUndo = _markdownEditor.CanUndo;
            viewModel.CanRedo = _markdownEditor.CanRedo;
            ApplyTasksPanelVisibility(viewModel.ShowTasksPanel, viewModel.TasksWidth);
        }
    }

    private void OnEditorHistoryStateChanged(bool canUndo, bool canRedo)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        viewModel.CanUndo = canUndo;
        viewModel.CanRedo = canRedo;
    }

    private void WireViewModelCallbacks(MainWindowViewModel viewModel)
    {
        viewModel.UndoRequested = () =>
        {
            _markdownEditor?.Undo();
            return Task.CompletedTask;
        };
        viewModel.RedoRequested = () =>
        {
            _markdownEditor?.Redo();
            return Task.CompletedTask;
        };
        viewModel.CutRequested = () => _markdownEditor?.CutSelectionAsync() ?? Task.CompletedTask;
        viewModel.CopyRequested = () => _markdownEditor?.CopySelectionAsync() ?? Task.CompletedTask;
        viewModel.PasteRequested = () => _markdownEditor?.PasteAsync() ?? Task.CompletedTask;
        viewModel.SelectAllRequested = () =>
        {
            _markdownEditor?.SelectAllText();
            return Task.CompletedTask;
        };
        viewModel.FindRequested = ShowFindDialogAsync;
        viewModel.ReplaceRequested = ShowReplaceDialogAsync;
        viewModel.PrintRequested = ShowPrintPreviewAsync;
        viewModel.ShowSettingsRequested = ShowSettingsDialogAsync;
        viewModel.ConfirmCloseNotepadAsync = ConfirmCloseNotepadAsync;
        viewModel.ConfirmExitAsync = ConfirmExitAsync;
        viewModel.ExportFilePathRequestedAsync = RequestExportFilePathAsync;
    }

    private async Task<string?> RequestExportFilePathAsync(string suggestedFileName, string format)
    {
        if (DataContext is not MainWindowViewModel)
        {
            return null;
        }

        var fileType = format.ToLowerInvariant() switch
        {
            "html" => "html",
            "text" => "txt",
            _ => "md"
        };

        var file = await StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            SuggestedFileName = suggestedFileName,
            FileTypeChoices = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType(format)
                {
                    Patterns = new[] { $"*.{fileType}" }
                }
            }
        });

        return file?.Path.LocalPath;
    }

    private void ApplyTasksPanelVisibility(bool isVisible, double savedWidth)
    {
        if (_contentGrid == null || _taskGridSplitter == null || _tasksPanelBorder == null || _contentGrid.ColumnDefinitions.Count < 3)
        {
            return;
        }

        var taskSplitterColumn = _contentGrid.ColumnDefinitions[1];
        var tasksColumn = _contentGrid.ColumnDefinitions[2];
        _taskGridSplitter.IsVisible = isVisible;
        _tasksPanelBorder.IsVisible = isVisible;
        taskSplitterColumn.Width = isVisible ? new GridLength(4) : new GridLength(0);
        tasksColumn.Width = isVisible ? new GridLength(Math.Max(150, savedWidth), GridUnitType.Pixel) : new GridLength(0);
    }

    private async Task ShowFindDialogAsync()
    {
        if (_markdownEditor == null)
        {
            return;
        }

        var searchBox = new TextBox { Width = 320 };
        var matchCaseBox = new CheckBox { Content = "Match case" };
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = "Find text:" });
        panel.Children.Add(searchBox);
        panel.Children.Add(matchCaseBox);

        var result = await ShowDialogAsync("Find", "Search within the current notepad.", panel, "Find Next", null, "Cancel");
        if (result != DialogResult.Primary || string.IsNullOrWhiteSpace(searchBox.Text))
        {
            return;
        }

        if (!_markdownEditor.FindNext(searchBox.Text, matchCaseBox.IsChecked == true))
        {
            await ShowInfoDialogAsync($"No match found for '{searchBox.Text}'.");
        }
    }

    private async Task ShowReplaceDialogAsync()
    {
        if (_markdownEditor == null)
        {
            return;
        }

        var searchBox = new TextBox { Width = 320 };
        var replacementBox = new TextBox { Width = 320 };
        var matchCaseBox = new CheckBox { Content = "Match case" };
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = "Find text:" });
        panel.Children.Add(searchBox);
        panel.Children.Add(new TextBlock { Text = "Replace with:" });
        panel.Children.Add(replacementBox);
        panel.Children.Add(matchCaseBox);

        var result = await ShowDialogAsync("Replace", "Replace within the current notepad.", panel, "Replace", "Replace All", "Cancel");
        if (result == DialogResult.Cancel || string.IsNullOrWhiteSpace(searchBox.Text))
        {
            return;
        }

        var matchCase = matchCaseBox.IsChecked == true;
        if (result == DialogResult.Secondary)
        {
            var replacements = _markdownEditor.ReplaceAll(searchBox.Text, replacementBox.Text ?? string.Empty, matchCase);
            await ShowInfoDialogAsync(replacements > 0
                ? $"Replaced {replacements} occurrence(s)."
                : $"No match found for '{searchBox.Text}'.");
            return;
        }

        if (!_markdownEditor.ReplaceCurrentSelection(searchBox.Text, replacementBox.Text ?? string.Empty, matchCase))
        {
            await ShowInfoDialogAsync($"No match found for '{searchBox.Text}'.");
        }
    }

    private Task ShowPrintPreviewAsync()
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return Task.CompletedTask;
        }

        var preview = new TextBox
        {
            Text = BuildPrintPreview(viewModel.CurrentNotepad),
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Width = 500,
            Height = 420
        };

        return ShowDialogAsync("Print Preview", "Preview the local note/task content before printing or copying.", preview, "Close", null);
    }

    private async Task ShowSettingsDialogAsync()
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var autoSaveBox = new CheckBox { Content = "Auto Save", IsChecked = viewModel.AutoSave };
        var showCompletedBox = new CheckBox { Content = "Show Completed Tasks", IsChecked = viewModel.ShowCompleted };
        var wordWrapBox = new CheckBox { Content = "Word Wrap", IsChecked = viewModel.WordWrapEnabled };
        var autoMarkdownBox = new CheckBox { Content = "Auto Markdown", IsChecked = viewModel.AutoMarkdownEnabled };
        var statusBarBox = new CheckBox { Content = "Show Status Bar", IsChecked = viewModel.ShowStatusBar };
        var tasksPanelBox = new CheckBox { Content = "Show Tasks Panel", IsChecked = viewModel.ShowTasksPanel };

        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(autoSaveBox);
        panel.Children.Add(showCompletedBox);
        panel.Children.Add(wordWrapBox);
        panel.Children.Add(autoMarkdownBox);
        panel.Children.Add(statusBarBox);
        panel.Children.Add(tasksPanelBox);

        var result = await ShowDialogAsync("Settings", "Local preferences for this lightweight workspace.", panel, "Save", null, "Cancel");
        if (result != DialogResult.Primary)
        {
            return;
        }

        viewModel.AutoSave = autoSaveBox.IsChecked == true;
        viewModel.ShowCompleted = showCompletedBox.IsChecked == true;
        viewModel.WordWrapEnabled = wordWrapBox.IsChecked == true;
        viewModel.AutoMarkdownEnabled = autoMarkdownBox.IsChecked == true;
        viewModel.ShowStatusBar = statusBarBox.IsChecked == true;
        viewModel.ShowTasksPanel = tasksPanelBox.IsChecked == true;
    }

    private static string BuildPrintPreview(NotepadData notepad)
    {
        var notes = notepad.Content.PlainText ?? string.Empty;
        var tasks = notepad.Tasks.Count == 0
            ? "(No tasks)"
            : string.Join(Environment.NewLine, notepad.Tasks.Select(t => $"[{(t.Done ? 'x' : ' ')}] {t.Text}"));

        return $"{notepad.Title}{Environment.NewLine}{Environment.NewLine}Notes:{Environment.NewLine}{notes}{Environment.NewLine}{Environment.NewLine}Tasks:{Environment.NewLine}{tasks}";
    }

    private async Task<bool> ConfirmCloseNotepadAsync(NotepadData notepad)
    {
        if (!notepad.IsDirty || DataContext is not MainWindowViewModel viewModel)
        {
            return true;
        }

        var decision = await ShowUnsavedChangesDialogAsync(notepad.Title);
        if (decision == UnsavedChangesDecision.Cancel)
        {
            return false;
        }

        if (decision == UnsavedChangesDecision.Save)
        {
            var previous = viewModel.CurrentNotepad;
            if (!ReferenceEquals(previous, notepad))
            {
                viewModel.CurrentNotepad = notepad;
            }

            if (viewModel.SaveCommand is IAsyncRelayCommand asyncCommand)
            {
                await asyncCommand.ExecuteAsync(null);
            }
            else
            {
                viewModel.SaveCommand.Execute(null);
            }

            if (!ReferenceEquals(previous, notepad))
            {
                viewModel.CurrentNotepad = previous;
            }

            return !notepad.IsDirty;
        }

        return true;
    }

    private async Task<bool> ConfirmExitAsync()
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return true;
        }

        foreach (var notepad in viewModel.Notepads)
        {
            if (!await ConfirmCloseNotepadAsync(notepad))
            {
                return false;
            }
        }

        return true;
    }

    private async Task<string?> ShowTextInputDialogAsync(string title, string prompt)
    {
        var input = new TextBox
        {
            Width = 320
        };

        var result = await ShowDialogAsync(title, prompt, input, "OK", "Cancel");
        return result == DialogResult.Primary ? input.Text : null;
    }

    private Task ShowInfoDialogAsync(string message)
    {
        return ShowDialogAsync("Tascade", message, null, "OK", null);
    }

    private async Task<UnsavedChangesDecision> ShowUnsavedChangesDialogAsync(string title)
    {
        var result = await ShowDialogAsync("Unsaved Changes", $"Save changes to '{title}' before closing?", null, "Save", "Discard", "Cancel");
        return result switch
        {
            DialogResult.Primary => UnsavedChangesDecision.Save,
            DialogResult.Secondary => UnsavedChangesDecision.Discard,
            _ => UnsavedChangesDecision.Cancel
        };
    }

    private async Task<DialogResult> ShowDialogAsync(string title, string message, Control? content, string primaryText, string? secondaryText, string? cancelText = null)
    {
        var tcs = new TaskCompletionSource<DialogResult>();
        var panel = new StackPanel
        {
            Spacing = 12,
            Margin = new Thickness(16)
        };
        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Width = 360
        });

        if (content != null)
        {
            panel.Children.Add(content);
        }

        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };

        void CloseDialog(Window dialog, DialogResult result)
        {
            if (!tcs.Task.IsCompleted)
            {
                tcs.SetResult(result);
            }

            dialog.Close();
        }

        var dialog = new Window
        {
            Title = title,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = panel
        };

        if (cancelText != null)
        {
            var cancelButton = new Button { Content = cancelText, MinWidth = 80 };
            cancelButton.Click += (_, _) => CloseDialog(dialog, DialogResult.Cancel);
            buttonRow.Children.Add(cancelButton);
        }

        if (secondaryText != null)
        {
            var secondaryButton = new Button { Content = secondaryText, MinWidth = 80 };
            secondaryButton.Click += (_, _) => CloseDialog(dialog, DialogResult.Secondary);
            buttonRow.Children.Add(secondaryButton);
        }

        var primaryButton = new Button { Content = primaryText, MinWidth = 80 };
        primaryButton.Click += (_, _) => CloseDialog(dialog, DialogResult.Primary);
        buttonRow.Children.Add(primaryButton);
        panel.Children.Add(buttonRow);

        dialog.Closed += (_, _) =>
        {
            if (!tcs.Task.IsCompleted)
            {
                tcs.SetResult(DialogResult.Cancel);
            }
        };

        await dialog.ShowDialog(this);
        return await tcs.Task;
    }

    private void OnAddTaskTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (viewModel.AddTaskCommand.CanExecute(null))
        {
            viewModel.AddTaskCommand.Execute(null);
            e.Handled = true;
        }
    }

    private enum DialogResult
    {
        Cancel,
        Primary,
        Secondary
    }

    private enum UnsavedChangesDecision
    {
        Cancel,
        Save,
        Discard
    }
}
