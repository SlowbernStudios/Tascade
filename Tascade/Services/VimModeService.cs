using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;

namespace Tascade.Services
{
    public enum VimMode
    {
        Normal,
        Insert,
        Command
    }

    public class VimModeService
    {
        private VimMode _currentMode = VimMode.Normal;
        private string _commandBuffer = string.Empty;
        private readonly TextBox _textBox;
        private AutoCompleteService? _autoCompleteService;

        public event Action<VimMode>? ModeChanged;
        public event Action<string>? CommandEntered;
        public event Action<string, int>? OnCompletionRequested;

        public VimMode CurrentMode
        {
            get => _currentMode;
            private set
            {
                if (_currentMode != value)
                {
                    _currentMode = value;
                    ModeChanged?.Invoke(_currentMode);
                }
            }
        }

        public string CommandBuffer
        {
            get => _commandBuffer;
            private set => _commandBuffer = value ?? string.Empty;
        }

        public AutoCompleteService? AutoCompleteService
        {
            get => _autoCompleteService;
            set => _autoCompleteService = value;
        }

        public VimModeService(TextBox textBox)
        {
            _textBox = textBox ?? throw new ArgumentNullException(nameof(textBox));
        }

        public bool HandleKeyInput(Key key, KeyModifiers modifiers)
        {
            return CurrentMode switch
            {
                VimMode.Normal => HandleNormalMode(key, modifiers),
                VimMode.Insert => HandleInsertMode(key, modifiers),
                VimMode.Command => HandleCommandMode(key, modifiers),
                _ => false
            };
        }

        private bool HandleNormalMode(Key key, KeyModifiers modifiers)
        {
            switch (key)
            {
                case Key.I:
                    CurrentMode = VimMode.Insert;
                    return true;
                case Key.A:
                    CurrentMode = VimMode.Insert;
                    return true;
                case Key.Oem1 when modifiers == KeyModifiers.Shift:
                    CurrentMode = VimMode.Command;
                    CommandBuffer = ":";
                    return true;
                case Key.Tab:
                case Key.N when modifiers == KeyModifiers.Control:
                case Key.P when modifiers == KeyModifiers.Control:
                    TriggerCompletion();
                    return true;
                case Key.Escape:
                    return true;
                default:
                    return false;
            }
        }

        private bool HandleInsertMode(Key key, KeyModifiers modifiers)
        {
            switch (key)
            {
                case Key.Escape:
                    CurrentMode = VimMode.Normal;
                    return true;
                case Key.Tab when modifiers == KeyModifiers.Control:
                case Key.N when modifiers == KeyModifiers.Control:
                case Key.P when modifiers == KeyModifiers.Control:
                    TriggerCompletion();
                    return true;
                default:
                    return false;
            }
        }

        private bool HandleCommandMode(Key key, KeyModifiers modifiers)
        {
            switch (key)
            {
                case Key.Escape:
                    CurrentMode = VimMode.Normal;
                    CommandBuffer = string.Empty;
                    return true;
                case Key.Enter:
                    ExecuteCommand();
                    return true;
                case Key.Back:
                    if (CommandBuffer.Length > 1)
                    {
                        CommandBuffer = CommandBuffer[..^1];
                    }
                    return true;
                case Key.Tab:
                    TriggerCommandCompletion();
                    return true;
            }

            if ((key >= Key.A && key <= Key.Z) || (key >= Key.D0 && key <= Key.D9) || key == Key.Space)
            {
                var charToAdd = GetCharFromKey(key, modifiers);
                if (charToAdd.HasValue)
                {
                    CommandBuffer += charToAdd.Value;
                    return true;
                }
            }

            return false;
        }

        private void TriggerCompletion()
        {
            if (_autoCompleteService == null)
            {
                return;
            }

            var text = _textBox.Text ?? string.Empty;
            var caret = _textBox.CaretIndex;
            if (_autoCompleteService.ShouldTriggerCompletion(text, caret))
            {
                OnCompletionRequested?.Invoke(text, caret);
            }
        }

        private void TriggerCommandCompletion()
        {
            if (_autoCompleteService == null)
            {
                return;
            }

            var suggestions = _autoCompleteService.GetSuggestions(CommandBuffer);
            var match = suggestions.FirstOrDefault(s => s.Type == SuggestionType.Command);
            if (match != null)
            {
                CommandBuffer = match.Text;
            }
        }

        private void ExecuteCommand()
        {
            var command = CommandBuffer.TrimStart(':').ToLowerInvariant();
            switch (command)
            {
                case "w":
                    CommandEntered?.Invoke("save");
                    break;
                case "q":
                    CommandEntered?.Invoke("quit");
                    break;
                case "wq":
                    CommandEntered?.Invoke("save");
                    CommandEntered?.Invoke("quit");
                    break;
                case "q!":
                    CommandEntered?.Invoke("quit!");
                    break;
            }

            CurrentMode = VimMode.Normal;
            CommandBuffer = string.Empty;
        }

        private static char? GetCharFromKey(Key key, KeyModifiers modifiers)
        {
            if (modifiers == KeyModifiers.Shift)
            {
                return key switch
                {
                    Key.D0 => ')',
                    Key.D1 => '!',
                    Key.D2 => '@',
                    Key.D3 => '#',
                    Key.D4 => '$',
                    Key.D5 => '%',
                    Key.D6 => '^',
                    Key.D7 => '&',
                    Key.D8 => '*',
                    Key.D9 => '(',
                    _ => null
                };
            }

            return key switch
            {
                Key.Space => ' ',
                Key.D0 => '0',
                Key.D1 => '1',
                Key.D2 => '2',
                Key.D3 => '3',
                Key.D4 => '4',
                Key.D5 => '5',
                Key.D6 => '6',
                Key.D7 => '7',
                Key.D8 => '8',
                Key.D9 => '9',
                >= Key.A and <= Key.Z => (char)('a' + (key - Key.A)),
                _ => null
            };
        }
    }
}
