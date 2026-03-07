using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Avalonia.Media;
using Tascade.Models;
using Tascade.Services;

namespace Tascade.Controls
{
    public partial class MarkdownEditor : UserControl
    {
        private RichTextContent _content = new();
        private int _lastSelectionStart;
        private int _lastSelectionLength;
        private bool _autoSplitTriggered;
        private bool _isUpdatingViewMode;

        public event Action<bool, bool>? HistoryStateChanged;

        public static readonly StyledProperty<bool> AutoMarkdownEnabledProperty =
            AvaloniaProperty.Register<MarkdownEditor, bool>(nameof(AutoMarkdownEnabled), defaultValue: true);

        public static readonly StyledProperty<ViewMode> ViewModeProperty =
            AvaloniaProperty.Register<MarkdownEditor, ViewMode>(nameof(ViewMode), defaultValue: ViewMode.Plain);

        public static readonly StyledProperty<AutoCompleteService?> AutoCompleteServiceProperty =
            AvaloniaProperty.Register<MarkdownEditor, AutoCompleteService?>(nameof(AutoCompleteService));

        public static readonly StyledProperty<VimModeService?> VimModeServiceProperty =
            AvaloniaProperty.Register<MarkdownEditor, VimModeService?>(nameof(VimModeService));

        public static readonly StyledProperty<bool> WordWrapEnabledProperty =
            AvaloniaProperty.Register<MarkdownEditor, bool>(nameof(WordWrapEnabled), defaultValue: true);

        public static readonly StyledProperty<double> ZoomLevelProperty =
            AvaloniaProperty.Register<MarkdownEditor, double>(nameof(ZoomLevel), defaultValue: 1.0);

        public bool AutoMarkdownEnabled
        {
            get => GetValue(AutoMarkdownEnabledProperty);
            set => SetValue(AutoMarkdownEnabledProperty, value);
        }

        public ViewMode ViewMode
        {
            get => GetValue(ViewModeProperty);
            set => SetValue(ViewModeProperty, value);
        }

        public AutoCompleteService? AutoCompleteService
        {
            get => GetValue(AutoCompleteServiceProperty);
            set => SetValue(AutoCompleteServiceProperty, value);
        }

        public VimModeService? VimModeService
        {
            get => GetValue(VimModeServiceProperty);
            set => SetValue(VimModeServiceProperty, value);
        }

        public bool WordWrapEnabled
        {
            get => GetValue(WordWrapEnabledProperty);
            set => SetValue(WordWrapEnabledProperty, value);
        }

        public double ZoomLevel
        {
            get => GetValue(ZoomLevelProperty);
            set => SetValue(ZoomLevelProperty, value);
        }

        public MarkdownEditor()
        {
            InitializeComponent();
            DataContext = _content;
            SetupToolbarEvents();
            ApplyViewMode();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ViewModeProperty && !_isUpdatingViewMode)
            {
                ApplyViewMode();
            }

            if (change.Property == AutoCompleteServiceProperty || change.Property == VimModeServiceProperty)
            {
                ApplyEditorServices();
            }

            if (change.Property == WordWrapEnabledProperty || change.Property == ZoomLevelProperty)
            {
                ApplyEditorPresentation();
            }
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
                NotifyHistoryStateChanged();
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
                combo.SelectionChanged += (_, _) => OnViewModeSelectionChanged();
            }

            var textBox = GetEditorTextBox();
            if (textBox != null)
            {
                textBox.TextChanged += OnEditorTextChanged;
                textBox.KeyUp += OnEditorSelectionHostChanged;
                textBox.PointerReleased += OnEditorSelectionHostChanged;
            }

            ApplyEditorServices();
            ApplyEditorPresentation();
        }

        private void ApplyViewMode()
        {
            var combo = this.FindControl<ComboBox>("ViewModeComboBox");
            var editor = this.FindControl<Border>("EditorBorder");
            var preview = this.FindControl<Border>("PreviewBorder");
            var splitter = this.FindControl<GridSplitter>("Splitter");
            var mainGrid = this.FindControl<Grid>("MainGrid");

            var mode = ViewMode switch
            {
                ViewMode.Plain => 0,
                ViewMode.Markdown => 1,
                ViewMode.Split => 2,
                _ => 0
            };

            if (editor == null || preview == null || splitter == null || mainGrid == null)
            {
                return;
            }

            if (combo != null && combo.SelectedIndex != mode)
            {
                combo.SelectedIndex = mode;
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

        private void OnViewModeSelectionChanged()
        {
            var combo = this.FindControl<ComboBox>("ViewModeComboBox");
            if (combo == null)
            {
                return;
            }

            var selectedMode = combo.SelectedIndex switch
            {
                1 => ViewMode.Markdown,
                2 => ViewMode.Split,
                _ => ViewMode.Plain
            };

            if (ViewMode != selectedMode)
            {
                _isUpdatingViewMode = true;
                ViewMode = selectedMode;
                _isUpdatingViewMode = false;
            }

            ApplyViewMode();
        }

        private void WrapSelection(string prefix, string suffix)
        {
            var textBox = GetEditorTextBox();
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
            var textBox = GetEditorTextBox();
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
            var textBox = GetEditorTextBox();
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
            NotifyHistoryStateChanged();
        }

        private void NotifyHistoryStateChanged()
        {
            HistoryStateChanged?.Invoke(CanUndo, CanRedo);
        }

        private void ApplyAutoSplitIfMarkdown()
        {
            if (_autoSplitTriggered || !AutoMarkdownEnabled)
            {
                return;
            }

            var combo = this.FindControl<ComboBox>("ViewModeComboBox");
            var textBox = GetEditorTextBox();
            if (combo == null || textBox == null)
            {
                return;
            }

            if (combo.SelectedIndex == 0 && IsLikelyMarkdown(textBox.Text))
            {
                ViewMode = ViewMode.Split;
                _autoSplitTriggered = true;
                ApplyViewMode();
            }
        }

        private AutoCompleteTextBox? GetAutoCompleteEditor()
        {
            return this.FindControl<AutoCompleteTextBox>("MarkdownTextBox");
        }

        private TextBox? GetEditorTextBox()
        {
            return GetAutoCompleteEditor()?.EditorTextBox;
        }

        private void ApplyEditorServices()
        {
            var editor = GetAutoCompleteEditor();
            if (editor == null)
            {
                return;
            }

            editor.AutoCompleteService = AutoCompleteService!;
            editor.VimModeService = VimModeService;
        }

        private void ApplyEditorPresentation()
        {
            var textBox = GetEditorTextBox();
            if (textBox == null)
            {
                return;
            }

            textBox.TextWrapping = WordWrapEnabled ? TextWrapping.Wrap : TextWrapping.NoWrap;
            textBox.FontSize = Math.Max(8, 14 * ZoomLevel);
        }

        public bool CanUndo => GetEditorTextBox()?.CanUndo ?? false;

        public bool CanRedo => GetEditorTextBox()?.CanRedo ?? false;

        public void Undo()
        {
            GetEditorTextBox()?.Undo();
            NotifyHistoryStateChanged();
        }

        public void Redo()
        {
            GetEditorTextBox()?.Redo();
            NotifyHistoryStateChanged();
        }

        public void SelectAllText()
        {
            GetEditorTextBox()?.SelectAll();
        }

        public async System.Threading.Tasks.Task CopySelectionAsync()
        {
            var textBox = GetEditorTextBox();
            var selectedText = textBox?.SelectedText;
            if (textBox == null || string.IsNullOrEmpty(selectedText))
            {
                return;
            }

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(selectedText);
            }
        }

        public async System.Threading.Tasks.Task CutSelectionAsync()
        {
            var textBox = GetEditorTextBox();
            if (textBox == null)
            {
                return;
            }

            var selectedText = textBox.SelectedText;
            if (string.IsNullOrEmpty(selectedText))
            {
                return;
            }

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(selectedText);
            }

            ReplaceSelection(textBox, string.Empty, textBox.SelectionStart, selectedText.Length);
        }

        public async System.Threading.Tasks.Task PasteAsync()
        {
            var textBox = GetEditorTextBox();
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (textBox == null || clipboard == null)
            {
                return;
            }

            var text = await clipboard.TryGetTextAsync();
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            ReplaceSelection(textBox, text, textBox.SelectionStart, textBox.SelectionEnd - textBox.SelectionStart);
        }

        public bool FindNext(string searchText, bool matchCase = false)
        {
            var textBox = GetEditorTextBox();
            var text = textBox?.Text;
            if (textBox == null || string.IsNullOrEmpty(text) || string.IsNullOrEmpty(searchText))
            {
                return false;
            }

            var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            var selectionEnd = Math.Max(textBox.SelectionEnd, textBox.CaretIndex);
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

            textBox.Focus();
            textBox.SelectionStart = index;
            textBox.SelectionEnd = index + searchText.Length;
            textBox.CaretIndex = index + searchText.Length;
            return true;
        }

        public bool ReplaceCurrentSelection(string searchText, string replacement, bool matchCase = false)
        {
            var textBox = GetEditorTextBox();
            if (textBox == null || string.IsNullOrEmpty(searchText))
            {
                return false;
            }

            var selectedText = textBox.SelectedText ?? string.Empty;
            var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            if (!string.Equals(selectedText, searchText, comparison))
            {
                if (!FindNext(searchText, matchCase))
                {
                    return false;
                }
            }

            var start = textBox.SelectionStart;
            var length = Math.Max(0, textBox.SelectionEnd - textBox.SelectionStart);
            ReplaceSelection(textBox, replacement, start, length);
            textBox.SelectionStart = start;
            textBox.SelectionEnd = start + replacement.Length;
            return true;
        }

        public int ReplaceAll(string searchText, string replacement, bool matchCase = false)
        {
            var textBox = GetEditorTextBox();
            var text = textBox?.Text;
            if (textBox == null || string.IsNullOrEmpty(text) || string.IsNullOrEmpty(searchText))
            {
                return 0;
            }

            var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            var current = text;
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
                textBox.Text = current;
                textBox.CaretIndex = Math.Min(current.Length, startIndex);
            }

            return count;
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

