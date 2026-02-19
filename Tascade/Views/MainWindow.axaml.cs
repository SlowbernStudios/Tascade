using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Tascade.Controls;
using Tascade.ViewModels;

namespace Tascade.Views;

public partial class MainWindow : Window
{
    private MarkdownEditor? _markdownEditor;
    private TextBox? _addTaskTextBox;

    public MainWindow()
    {
        InitializeComponent();
        
        // Get reference to the markdown editor
        _markdownEditor = this.FindControl<MarkdownEditor>("MarkdownEditor");
        _addTaskTextBox = this.FindControl<TextBox>("AddTaskTextBox");
        if (_addTaskTextBox != null)
        {
            _addTaskTextBox.KeyDown += OnAddTaskTextBoxKeyDown;
        }
        
        // Subscribe to data context changes to update the editor
        DataContextChanged += OnDataContextChanged;
        
        // Initialize file operations when the window is opened
        Opened += OnWindowOpened;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            // Initialize file operations with the TopLevel (window)
            viewModel.InitializeFileOperations(this);
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel && _markdownEditor != null)
        {
            // Update the markdown editor when the current notepad changes
            viewModel.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(viewModel.CurrentNotepad))
                {
                    _markdownEditor.Content = viewModel.CurrentNotepad.Content;
                }
            };
            
            // Set initial content
            _markdownEditor.Content = viewModel.CurrentNotepad.Content;
            
            // Initialize file operations if not already done
            if (viewModel != null)
            {
                viewModel.InitializeFileOperations(this);
            }
        }
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
}
