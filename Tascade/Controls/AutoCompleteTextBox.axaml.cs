using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Tascade.Models;
using Tascade.Services;

namespace Tascade.Controls
{
    public partial class AutoCompleteTextBox : UserControl, INotifyPropertyChanged
    {
        public static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<AutoCompleteTextBox, string>(nameof(Text), defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<AutoCompleteService> AutoCompleteServiceProperty =
            AvaloniaProperty.Register<AutoCompleteTextBox, AutoCompleteService>(nameof(AutoCompleteService));

        public static readonly StyledProperty<VimModeService?> VimModeServiceProperty =
            AvaloniaProperty.Register<AutoCompleteTextBox, VimModeService?>(nameof(VimModeService));

        public static readonly StyledProperty<bool> IsCompletionVisibleProperty =
            AvaloniaProperty.Register<AutoCompleteTextBox, bool>(nameof(IsCompletionVisible));

        public static readonly StyledProperty<int> SelectedSuggestionIndexProperty =
            AvaloniaProperty.Register<AutoCompleteTextBox, int>(nameof(SelectedSuggestionIndex));

        private string _lastText = "";
        private int _lastCursorPosition = 0;
        private int _currentWordStart = 0;
        private List<SuggestionItem> _suggestions = new();

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public AutoCompleteService AutoCompleteService
        {
            get => GetValue(AutoCompleteServiceProperty);
            set => SetValue(AutoCompleteServiceProperty, value);
        }

        public VimModeService? VimModeService
        {
            get => GetValue(VimModeServiceProperty);
            set => SetValue(VimModeServiceProperty, value);
        }

        public bool IsCompletionVisible
        {
            get => GetValue(IsCompletionVisibleProperty);
            set => SetValue(IsCompletionVisibleProperty, value);
        }

        public int SelectedSuggestionIndex
        {
            get => GetValue(SelectedSuggestionIndexProperty);
            set => SetValue(SelectedSuggestionIndexProperty, value);
        }

        public TextBox EditorTextBox => MainTextBox;

        public List<SuggestionItem> Suggestions
        {
            get => _suggestions;
            private set
            {
                _suggestions = value;
                OnPropertyChanged(nameof(Suggestions));
            }
        }

        public AutoCompleteTextBox()
        {
            InitializeComponent();
            
            // Set up event handlers
            MainTextBox.KeyDown += OnMainTextBoxKeyDown;
            MainTextBox.TextInput += OnMainTextBoxTextInput;
            MainTextBox.LostFocus += OnMainTextBoxLostFocus;
            
            CompletionListBox.KeyDown += OnCompletionListBoxKeyDown;
            CompletionListBox.DoubleTapped += OnCompletionListBoxDoubleTapped;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            
            if (e.Property == TextProperty)
            {
                OnTextChanged(e.NewValue?.ToString() ?? "");
            }
            else if (e.Property == SelectedSuggestionIndexProperty)
            {
                OnSelectedSuggestionIndexChanged((int)e.NewValue!);
            }
            else if (e.Property == VimModeServiceProperty)
            {
                OnVimModeServiceChanged(e.NewValue as VimModeService);
            }
        }

        private void OnVimModeServiceChanged(VimModeService? vimService)
        {
            if (vimService != null)
            {
                // Connect the AutoCompleteService to VimModeService
                vimService.AutoCompleteService = AutoCompleteService;
                
                // Subscribe to completion requests from VimModeService
                vimService.OnCompletionRequested += (text, cursorPosition) =>
                {
                    ShowCompletion(text, cursorPosition);
                };
            }
        }

        private void OnTextChanged(string newText)
        {
            if (AutoCompleteService == null)
                return;

            var cursorPosition = MainTextBox?.CaretIndex ?? 0;
            var currentText = Text ?? "";

            // Update word completion service with all words in the text
            if (AutoCompleteService != null)
            {
                var words = Regex.Matches(currentText, @"\b\w+\b")
                    .Cast<Match>()
                    .Select(m => m.Value)
                    .Distinct();
                AutoCompleteService.UpdateWords(words);
            }

            // Check if completion should be triggered
            if (AutoCompleteService?.ShouldTriggerCompletion(currentText, cursorPosition) == true)
            {
                ShowCompletion(currentText, cursorPosition);
            }
            else
            {
                HideCompletion();
            }

            _lastText = currentText;
            _lastCursorPosition = cursorPosition;
        }

        private void OnMainTextBoxKeyDown(object? sender, KeyEventArgs e)
        {
            // Handle global completion shortcuts
            if (e.Key == Key.Space && e.KeyModifiers == KeyModifiers.Control)
            {
                TriggerManualCompletion();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.N && e.KeyModifiers == KeyModifiers.Control)
            {
                TriggerManualCompletion();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.P && e.KeyModifiers == KeyModifiers.Control)
            {
                TriggerManualCompletion();
                e.Handled = true;
                return;
            }

            if (!IsCompletionVisible)
                return;

            switch (e.Key)
            {
                case Key.Escape:
                    HideCompletion();
                    e.Handled = true;
                    break;
                    
                case Key.Enter:
                    if (SelectedSuggestionIndex >= 0 && SelectedSuggestionIndex < Suggestions.Count)
                    {
                        ApplySuggestion(Suggestions[SelectedSuggestionIndex]);
                        e.Handled = true;
                    }
                    break;
                    
                case Key.Tab:
                    if (SelectedSuggestionIndex >= 0 && SelectedSuggestionIndex < Suggestions.Count)
                    {
                        ApplySuggestion(Suggestions[SelectedSuggestionIndex]);
                        e.Handled = true;
                    }
                    else if (Suggestions.Count > 0)
                    {
                        ApplySuggestion(Suggestions[0]);
                        e.Handled = true;
                    }
                    break;
                    
                case Key.Up:
                    if (SelectedSuggestionIndex > 0)
                    {
                        SelectedSuggestionIndex--;
                        e.Handled = true;
                    }
                    break;
                    
                case Key.Down:
                    if (SelectedSuggestionIndex < Suggestions.Count - 1)
                    {
                        SelectedSuggestionIndex++;
                        e.Handled = true;
                    }
                    break;
                    
                case Key.Left:
                case Key.Right:
                case Key.Home:
                case Key.End:
                    // Navigation keys hide completion
                    HideCompletion();
                    break;
            }
        }

        private void TriggerManualCompletion()
        {
            var cursorPosition = MainTextBox.CaretIndex;
            var currentText = Text ?? "";
            ShowCompletion(currentText, cursorPosition);
        }

        private void OnMainTextBoxTextInput(object? sender, TextInputEventArgs e)
        {
            // Let normal text input through, but update completion
            if (IsCompletionVisible)
            {
                var cursorPosition = MainTextBox.CaretIndex;
                var currentText = Text ?? "";
                ShowCompletion(currentText, cursorPosition);
            }
        }

        private void OnMainTextBoxLostFocus(object? sender, RoutedEventArgs e)
        {
            // Small delay to allow clicking on completion items
            HideCompletion();
        }

        private void OnCompletionListBoxKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                if (SelectedSuggestionIndex >= 0 && SelectedSuggestionIndex < Suggestions.Count)
                {
                    ApplySuggestion(Suggestions[SelectedSuggestionIndex]);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Escape)
            {
                HideCompletion();
                e.Handled = true;
            }
        }

        private void OnCompletionListBoxDoubleTapped(object? sender, RoutedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is SuggestionItem suggestion)
            {
                ApplySuggestion(suggestion);
            }
        }

        private void OnSelectedSuggestionIndexChanged(int newIndex)
        {
            // Scroll the selected item into view
            if (newIndex >= 0 && newIndex < Suggestions.Count)
            {
                CompletionListBox.ScrollIntoView(Suggestions[newIndex]);
            }
        }

        private void ShowCompletion(string text, int cursorPosition)
        {
            if (AutoCompleteService == null)
            {
                HideCompletion();
                return;
            }

            var wordBeforeCursor = GetWordBeforeCursor(text, cursorPosition);
            if (string.IsNullOrEmpty(wordBeforeCursor))
            {
                HideCompletion();
                return;
            }

            var suggestions = AutoCompleteService.GetSuggestions(wordBeforeCursor);
            if (!suggestions.Any())
            {
                HideCompletion();
                return;
            }

            Suggestions = suggestions;
            SelectedSuggestionIndex = 0;
            _currentWordStart = GetWordStart(text, cursorPosition);
            IsCompletionVisible = true;
        }

        private void HideCompletion()
        {
            IsCompletionVisible = false;
            Suggestions = new List<SuggestionItem>();
            SelectedSuggestionIndex = -1;
        }

        private void ApplySuggestion(SuggestionItem suggestion)
        {
            var text = Text ?? "";
            var cursorPosition = MainTextBox.CaretIndex;
            var wordStart = GetWordStart(text, cursorPosition);
            var wordEnd = GetWordEnd(text, cursorPosition);
            
            var newText = text.Substring(0, wordStart) + suggestion.Text + text.Substring(wordEnd);
            var newCursorPosition = wordStart + suggestion.Text.Length;
            
            Text = newText;
            MainTextBox.CaretIndex = newCursorPosition;
            
            HideCompletion();
        }

        private string GetWordBeforeCursor(string text, int cursorPosition)
        {
            var start = cursorPosition - 1;
            while (start >= 0 && IsWordCharacter(text[start]))
            {
                start--;
            }
            return text.Substring(start + 1, cursorPosition - start - 1);
        }

        private int GetWordStart(string text, int cursorPosition)
        {
            var start = cursorPosition - 1;
            while (start >= 0 && IsWordCharacter(text[start]))
            {
                start--;
            }
            return start + 1;
        }

        private int GetWordEnd(string text, int cursorPosition)
        {
            var end = cursorPosition;
            while (end < text.Length && IsWordCharacter(text[end]))
            {
                end++;
            }
            return end;
        }

        private bool IsCursorInWord(string text, int cursorPosition, int wordStart)
        {
            var currentWordStart = GetWordStart(text, cursorPosition);
            return currentWordStart == wordStart;
        }

        private static bool IsWordCharacter(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_' || c == '/' || c == '\\';
        }
    }
}
