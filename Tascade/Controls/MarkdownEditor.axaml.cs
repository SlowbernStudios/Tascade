using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Tascade.Models;

namespace Tascade.Controls
{
    public partial class MarkdownEditor : UserControl
    {
        private RichTextContent _content = new();
        private int _lastSelectionStart;
        private int _lastSelectionLength;
        private bool _autoSplitTriggered;

        public MarkdownEditor()
        {
            InitializeComponent();
            DataContext = _content;
            SetupToolbarEvents();
            ApplyViewMode();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public new RichTextContent Content
        {
            get => _content;
            set
            {
                _content = value ?? new RichTextContent();
                DataContext = _content;
                _autoSplitTriggered = false;
                ApplyAutoSplitIfMarkdown();
            }
        }

        private void SetupToolbarEvents()
        {
            this.FindControl<Button>("BoldButton")!.Click += (_, _) => WrapSelection("**", "**");
            this.FindControl<Button>("ItalicButton")!.Click += (_, _) => WrapSelection("*", "*");
            this.FindControl<Button>("CodeButton")!.Click += (_, _) => WrapSelection("`", "`");
            this.FindControl<Button>("StrikeButton")!.Click += (_, _) => WrapSelection("~~", "~~");

            this.FindControl<Button>("H1Button")!.Click += (_, _) => PrefixLine("# ");
            this.FindControl<Button>("H2Button")!.Click += (_, _) => PrefixLine("## ");
            this.FindControl<Button>("H3Button")!.Click += (_, _) => PrefixLine("### ");
            this.FindControl<Button>("BulletListButton")!.Click += (_, _) => PrefixLine("- ");
            this.FindControl<Button>("NumberListButton")!.Click += (_, _) => PrefixLine("1. ");
            this.FindControl<Button>("TaskListButton")!.Click += (_, _) => PrefixLine("- [ ] ");
            this.FindControl<Button>("LinkButton")!.Click += (_, _) => InsertText("[text](https://)");
            this.FindControl<Button>("ImageButton")!.Click += (_, _) => InsertText("![alt](image.png)");
            this.FindControl<Button>("TableButton")!.Click += (_, _) => InsertText("| Col 1 | Col 2 |\n|---|---|\n| A | B |\n");
            this.FindControl<Button>("QuoteButton")!.Click += (_, _) => PrefixLine("> ");
            this.FindControl<Button>("CodeBlockButton")!.Click += (_, _) => InsertText("```\ncode\n```\n");
            this.FindControl<Button>("HrButton")!.Click += (_, _) => InsertText("\n---\n");

            var combo = this.FindControl<ComboBox>("ViewModeComboBox");
            if (combo != null)
            {
                combo.SelectionChanged += (_, _) => ApplyViewMode();
            }

            var textBox = this.FindControl<TextBox>("MarkdownTextBox");
            if (textBox != null)
            {
                textBox.TextChanged += OnEditorTextChanged;
                textBox.KeyUp += OnEditorSelectionHostChanged;
                textBox.PointerReleased += OnEditorSelectionHostChanged;
            }
        }

        private void ApplyViewMode()
        {
            var combo = this.FindControl<ComboBox>("ViewModeComboBox");
            var editor = this.FindControl<Border>("EditorBorder");
            var preview = this.FindControl<Border>("PreviewBorder");
            var splitter = this.FindControl<GridSplitter>("Splitter");
            var mainGrid = this.FindControl<Grid>("MainGrid");

            var mode = combo?.SelectedIndex ?? 0;

            if (editor == null || preview == null || splitter == null || mainGrid == null)
            {
                return;
            }

            editor.IsVisible = mode is 0 or 2;
            preview.IsVisible = mode is 1 or 2;
            splitter.IsVisible = mode == 2;

            if (mainGrid.ColumnDefinitions.Count >= 2)
            {
                if (mode == 0)
                {
                    mainGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                    mainGrid.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel);
                }
                else if (mode == 1)
                {
                    mainGrid.ColumnDefinitions[0].Width = new GridLength(0, GridUnitType.Pixel);
                    mainGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                }
                else
                {
                    mainGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                    mainGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                }
            }
        }

        private void WrapSelection(string prefix, string suffix)
        {
            var textBox = this.FindControl<TextBox>("MarkdownTextBox");
            if (textBox == null)
            {
                return;
            }

            var selected = textBox.SelectedText ?? string.Empty;
            GetSelectionRange(textBox, out var start, out var length, ref selected);
            if (string.IsNullOrEmpty(selected))
            {
                selected = "text";
            }

            ReplaceSelection(textBox, prefix + selected + suffix, start, length);
        }

        private void PrefixLine(string prefix)
        {
            var textBox = this.FindControl<TextBox>("MarkdownTextBox");
            if (textBox == null)
            {
                return;
            }

            var selected = textBox.SelectedText ?? string.Empty;
            GetSelectionRange(textBox, out var start, out var length, ref selected);
            ReplaceSelection(textBox, prefix + selected, start, length);
        }

        private void InsertText(string text)
        {
            var textBox = this.FindControl<TextBox>("MarkdownTextBox");
            if (textBox == null)
            {
                return;
            }

            var selected = textBox.SelectedText ?? string.Empty;
            GetSelectionRange(textBox, out var start, out var length, ref selected);
            ReplaceSelection(textBox, text, start, length);
        }

        private static void ReplaceSelection(TextBox textBox, string replacement, int start, int length)
        {
            var current = textBox.Text ?? string.Empty;
            start = Math.Clamp(start, 0, current.Length);
            var end = Math.Min(current.Length, start + Math.Max(0, length));
            textBox.Text = current[..start] + replacement + current[end..];
            textBox.CaretIndex = start + replacement.Length;
        }

        private void GetSelectionRange(TextBox textBox, out int start, out int length, ref string selectedText)
        {
            start = textBox.SelectionStart;
            length = selectedText.Length;

            if (length > 0)
            {
                _lastSelectionStart = start;
                _lastSelectionLength = length;
                return;
            }

            if (_lastSelectionLength > 0)
            {
                var current = textBox.Text ?? string.Empty;
                start = Math.Clamp(_lastSelectionStart, 0, current.Length);
                var end = Math.Clamp(_lastSelectionStart + _lastSelectionLength, 0, current.Length);
                if (end > start)
                {
                    length = end - start;
                    selectedText = current.Substring(start, length);
                }
            }
        }

        private void OnEditorSelectionHostChanged(object? sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null)
            {
                return;
            }

            var selected = textBox.SelectedText ?? string.Empty;
            if (selected.Length > 0)
            {
                _lastSelectionStart = textBox.SelectionStart;
                _lastSelectionLength = selected.Length;
            }
        }

        private void OnEditorTextChanged(object? sender, TextChangedEventArgs e)
        {
            ApplyAutoSplitIfMarkdown();
        }

        private void ApplyAutoSplitIfMarkdown()
        {
            if (_autoSplitTriggered)
            {
                return;
            }

            var combo = this.FindControl<ComboBox>("ViewModeComboBox");
            var textBox = this.FindControl<TextBox>("MarkdownTextBox");
            if (combo == null || textBox == null)
            {
                return;
            }

            if (combo.SelectedIndex == 0 && IsLikelyMarkdown(textBox.Text))
            {
                combo.SelectedIndex = 2;
                _autoSplitTriggered = true;
            }
        }

        private static bool IsLikelyMarkdown(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return text.Contains("# ")
                || text.Contains("## ")
                || text.Contains("**")
                || text.Contains("*")
                || text.Contains("`")
                || text.Contains("- ")
                || text.Contains("> ")
                || text.Contains("[")
                || text.Contains("](http")
                || text.Contains("```");
        }
    }
}

