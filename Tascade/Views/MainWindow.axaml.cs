using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Tascade.Models;
using Tascade.ViewModels;

namespace Tascade.Views;

public partial class MainWindow : Window
{
    private TextBox? _editorTextBox;
    private TextBox? _addTaskTextBox;
    private MainWindowViewModel? _subscribedViewModel;
    private Grid? _contentGrid;
    private GridSplitter? _taskGridSplitter;
    private Border? _tasksPanelBorder;
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();

        _editorTextBox = this.FindControl<TextBox>("EditorTextBox");
        _addTaskTextBox = this.FindControl<TextBox>("AddTaskTextBox");
        _contentGrid = this.FindControl<Grid>("ContentGrid");
        _taskGridSplitter = this.FindControl<GridSplitter>("TaskGridSplitter");
        _tasksPanelBorder = this.FindControl<Border>("TasksPanelBorder");

        if (_addTaskTextBox != null)
        {
            _addTaskTextBox.KeyDown += OnAddTaskTextBoxKeyDown;
        }

        if (_editorTextBox != null)
        {
            _editorTextBox.TextChanged += OnEditorStateChanged;
            _editorTextBox.KeyUp += OnEditorStateChanged;
        }

        if (_taskGridSplitter != null)
        {
            _taskGridSplitter.PointerReleased += OnTaskGridSplitterPointerReleased;
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
            ApplyEditorPresentation(viewModel);
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
        await viewModel.FlushPendingChangesAsync();
        _allowClose = true;
        Close();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (!ReferenceEquals(_subscribedViewModel, viewModel))
        {
            _subscribedViewModel = viewModel;
            WireViewModelCallbacks(viewModel);
            viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is nameof(viewModel.WordWrapEnabled) or nameof(viewModel.ZoomLevel))
                {
                    ApplyEditorPresentation(viewModel);
                }

                if (args.PropertyName == nameof(viewModel.ShowTasksPanel))
                {
                    ApplyTasksPanelVisibility(viewModel.ShowTasksPanel, viewModel.TasksWidth);
                }

                if (args.PropertyName == nameof(viewModel.TasksWidth))
                {
                    ApplyTasksPanelVisibility(viewModel.ShowTasksPanel, viewModel.TasksWidth);
                }
            };
        }

        viewModel.CanUndo = _editorTextBox?.CanUndo ?? false;
        viewModel.CanRedo = _editorTextBox?.CanRedo ?? false;
        ApplyEditorPresentation(viewModel);
        ApplyTasksPanelVisibility(viewModel.ShowTasksPanel, viewModel.TasksWidth);
    }

    private void WireViewModelCallbacks(MainWindowViewModel viewModel)
    {
        viewModel.UndoRequested = () =>
        {
            _editorTextBox?.Undo();
            return Task.CompletedTask;
        };
        viewModel.RedoRequested = () =>
        {
            _editorTextBox?.Redo();
            return Task.CompletedTask;
        };
        viewModel.CutRequested = async () =>
        {
            if (_editorTextBox != null)
            {
                await CopySelectionToClipboardAsync();
                ReplaceRange(_editorTextBox.SelectionStart, _editorTextBox.SelectionEnd - _editorTextBox.SelectionStart, string.Empty);
            }
        };
        viewModel.CopyRequested = async () =>
        {
            await CopySelectionToClipboardAsync();
        };
        viewModel.PasteRequested = async () =>
        {
            if (_editorTextBox == null)
            {
                return;
            }

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard == null)
            {
                return;
            }

            var text = await ClipboardExtensions.TryGetTextAsync(clipboard);
            if (!string.IsNullOrEmpty(text))
            {
                ReplaceRange(_editorTextBox.SelectionStart, _editorTextBox.SelectionEnd - _editorTextBox.SelectionStart, text);
            }
        };
        viewModel.SelectAllRequested = () =>
        {
            _editorTextBox?.SelectAll();
            return Task.CompletedTask;
        };
        viewModel.FindRequested = ShowFindDialogAsync;
        viewModel.ReplaceRequested = ShowReplaceDialogAsync;
        viewModel.PrintRequested = ShowPrintPreviewAsync;
        viewModel.ShowSettingsRequested = ShowSettingsDialogAsync;
    }

    private void ApplyEditorPresentation(MainWindowViewModel viewModel)
    {
        if (_editorTextBox == null)
        {
            return;
        }

        _editorTextBox.TextWrapping = viewModel.WordWrapEnabled ? Avalonia.Media.TextWrapping.Wrap : Avalonia.Media.TextWrapping.NoWrap;
        _editorTextBox.FontSize = Math.Max(8, 14 * viewModel.ZoomLevel);
    }

    private void OnEditorStateChanged(object? sender, EventArgs e)
    {
        UpdateEditorHistoryState();
    }

    private void UpdateEditorHistoryState()
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        viewModel.CanUndo = _editorTextBox?.CanUndo ?? false;
        viewModel.CanRedo = _editorTextBox?.CanRedo ?? false;
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

    private void OnTaskGridSplitterPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        SyncTasksWidthFromLayout();
    }

    private void SyncTasksWidthFromLayout()
    {
        if (DataContext is not MainWindowViewModel viewModel || _contentGrid == null || _contentGrid.ColumnDefinitions.Count < 3)
        {
            return;
        }

        var width = _contentGrid.ColumnDefinitions[2].Width;
        if (width.GridUnitType == GridUnitType.Pixel && width.Value >= 150)
        {
            var normalized = (float)Math.Round(width.Value, 2);
            if (Math.Abs(viewModel.TasksWidth - normalized) > 0.01f)
            {
                viewModel.TasksWidth = normalized;
            }
        }
    }

    private async Task ShowFindDialogAsync()
    {
        if (_editorTextBox == null)
        {
            return;
        }

        var searchBox = new TextBox { Width = 320 };
        var matchCaseBox = new CheckBox { Content = "Match case" };
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = "Find text:" });
        panel.Children.Add(searchBox);
        panel.Children.Add(matchCaseBox);

        var result = await ShowDialogAsync("Find", "Search within the current note.", panel, "Find Next", null, "Cancel");
        if (result != DialogResult.Primary || string.IsNullOrWhiteSpace(searchBox.Text))
        {
            return;
        }

        if (!FindNext(searchBox.Text, matchCaseBox.IsChecked == true))
        {
            await ShowInfoDialogAsync($"No match found for '{searchBox.Text}'.");
        }
    }

    private async Task ShowReplaceDialogAsync()
    {
        if (_editorTextBox == null)
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

        var result = await ShowDialogAsync("Replace", "Replace within the current note.", panel, "Replace", "Replace All", "Cancel");
        if (result == DialogResult.Cancel || string.IsNullOrWhiteSpace(searchBox.Text))
        {
            return;
        }

        var matchCase = matchCaseBox.IsChecked == true;
        if (result == DialogResult.Secondary)
        {
            var replacements = ReplaceAll(searchBox.Text, replacementBox.Text ?? string.Empty, matchCase);
            await ShowInfoDialogAsync(replacements > 0
                ? $"Replaced {replacements} occurrence(s)."
                : $"No match found for '{searchBox.Text}'.");
            return;
        }

        if (!ReplaceCurrentSelection(searchBox.Text, replacementBox.Text ?? string.Empty, matchCase))
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

        var showCompletedBox = new CheckBox { Content = "Show Completed Tasks", IsChecked = viewModel.ShowCompleted };
        var wordWrapBox = new CheckBox { Content = "Word Wrap", IsChecked = viewModel.WordWrapEnabled };
        var statusBarBox = new CheckBox { Content = "Show Status Bar", IsChecked = viewModel.ShowStatusBar };
        var tasksPanelBox = new CheckBox { Content = "Show Tasks Panel", IsChecked = viewModel.ShowTasksPanel };

        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(showCompletedBox);
        panel.Children.Add(wordWrapBox);
        panel.Children.Add(statusBarBox);
        panel.Children.Add(tasksPanelBox);

        var result = await ShowDialogAsync("Settings", "Local preferences for this lightweight workspace.", panel, "Save", null, "Cancel");
        if (result != DialogResult.Primary)
        {
            return;
        }

        viewModel.ShowCompleted = showCompletedBox.IsChecked == true;
        viewModel.WordWrapEnabled = wordWrapBox.IsChecked == true;
        viewModel.ShowStatusBar = statusBarBox.IsChecked == true;
        viewModel.ShowTasksPanel = tasksPanelBox.IsChecked == true;
    }

    private static string BuildPrintPreview(NotepadData notepad)
    {
        var notes = notepad.Text ?? string.Empty;
        var tasks = notepad.Tasks.Count == 0
            ? "(No tasks)"
            : string.Join(Environment.NewLine, notepad.Tasks.Select(t => $"[{(t.Done ? 'x' : ' ')}] {t.Text}"));

        return $"{notepad.Title}{Environment.NewLine}{Environment.NewLine}Notes:{Environment.NewLine}{notes}{Environment.NewLine}{Environment.NewLine}Tasks:{Environment.NewLine}{tasks}";
    }

    private bool FindNext(string searchText, bool matchCase)
    {
        if (_editorTextBox == null || string.IsNullOrEmpty(_editorTextBox.Text) || string.IsNullOrEmpty(searchText))
        {
            return false;
        }

        var text = _editorTextBox.Text;
        var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var selectionEnd = Math.Max(_editorTextBox.SelectionEnd, _editorTextBox.CaretIndex);
        var startIndex = Math.Clamp(selectionEnd, 0, text.Length);
        var index = text.IndexOf(searchText, startIndex, comparison);
        if (index < 0 && startIndex > 0)
        {
            index = text.IndexOf(searchText, 0, comparison);
        }

        if (index < 0)
        {
            return false;
        }

        _editorTextBox.Focus();
        _editorTextBox.SelectionStart = index;
        _editorTextBox.SelectionEnd = index + searchText.Length;
        _editorTextBox.CaretIndex = index + searchText.Length;
        return true;
    }

    private bool ReplaceCurrentSelection(string searchText, string replacement, bool matchCase)
    {
        if (_editorTextBox == null || string.IsNullOrEmpty(searchText))
        {
            return false;
        }

        var selectedText = _editorTextBox.SelectedText ?? string.Empty;
        var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        if (!string.Equals(selectedText, searchText, comparison))
        {
            if (!FindNext(searchText, matchCase))
            {
                return false;
            }
        }

        var start = _editorTextBox.SelectionStart;
        var length = Math.Max(0, _editorTextBox.SelectionEnd - _editorTextBox.SelectionStart);
        ReplaceRange(start, length, replacement);
        _editorTextBox.SelectionStart = start;
        _editorTextBox.SelectionEnd = start + replacement.Length;
        return true;
    }

    private int ReplaceAll(string searchText, string replacement, bool matchCase)
    {
        if (_editorTextBox == null || string.IsNullOrEmpty(_editorTextBox.Text) || string.IsNullOrEmpty(searchText))
        {
            return 0;
        }

        var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var current = _editorTextBox.Text;
        var count = 0;
        var startIndex = 0;

        while (startIndex <= current.Length)
        {
            var index = current.IndexOf(searchText, startIndex, comparison);
            if (index < 0)
            {
                break;
            }

            current = current[..index] + replacement + current[(index + searchText.Length)..];
            startIndex = index + replacement.Length;
            count++;
        }

        if (count > 0)
        {
            _editorTextBox.Text = current;
            _editorTextBox.CaretIndex = Math.Min(current.Length, startIndex);
        }

        return count;
    }

    private void ReplaceRange(int start, int length, string replacement)
    {
        if (_editorTextBox == null)
        {
            return;
        }

        var current = _editorTextBox.Text ?? string.Empty;
        start = Math.Clamp(start, 0, current.Length);
        var end = Math.Min(current.Length, start + Math.Max(0, length));
        _editorTextBox.Text = current[..start] + replacement + current[end..];
        _editorTextBox.CaretIndex = start + replacement.Length;
        UpdateEditorHistoryState();
    }

    private async Task CopySelectionToClipboardAsync()
    {
        if (_editorTextBox == null || string.IsNullOrEmpty(_editorTextBox.SelectedText))
        {
            return;
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(_editorTextBox.SelectedText);
        }
    }

    private Task ShowInfoDialogAsync(string message)
    {
        return ShowDialogAsync("Tascade", message, null, "OK", null);
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
}
