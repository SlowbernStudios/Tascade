using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;

namespace NotepadApp.Services
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
        
        public event Action<VimMode>? ModeChanged;
        public event Action<string>? CommandEntered;
        
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

        public VimModeService(TextBox textBox)
        {
            _textBox = textBox ?? throw new ArgumentNullException(nameof(textBox));
        }

        public bool HandleKeyInput(Key key, KeyModifiers modifiers)
        {
            switch (CurrentMode)
            {
                case VimMode.Normal:
                    return HandleNormalMode(key, modifiers);
                case VimMode.Insert:
                    return HandleInsertMode(key, modifiers);
                case VimMode.Command:
                    return HandleCommandMode(key, modifiers);
                default:
                    return false;
            }
        }

        private bool HandleNormalMode(Key key, KeyModifiers modifiers)
        {
            // Movement keys
            switch (key)
            {
                case Key.H:
                    MoveCursor(-1, 0);
                    return true;
                case Key.J:
                    MoveCursor(0, 1);
                    return true;
                case Key.K:
                    MoveCursor(0, -1);
                    return true;
                case Key.L:
                    MoveCursor(1, 0);
                    return true;
                case Key.W:
                    MoveWordForward();
                    return true;
                case Key.B:
                    MoveWordBackward();
                    return true;
                case Key.O:
                    if (modifiers == KeyModifiers.Shift)
                    {
                        EnterInsertModeAbove();
                    }
                    else
                    {
                        EnterInsertModeBelow();
                    }
                    return true;
                case Key.I:
                    CurrentMode = VimMode.Insert;
                    return true;
                case Key.A:
                    MoveCursor(1, 0);
                    CurrentMode = VimMode.Insert;
                    return true;
                case Key.D0 when modifiers == KeyModifiers.Shift: // Shift+0 (D)
                    DeleteToLineEnd();
                    return true;
                case Key.D when modifiers == KeyModifiers.Shift: // Shift+D
                    DeleteToLineEnd();
                    return true;
                case Key.D:
                    StartDeleteCommand();
                    return true;
                case Key.X:
                    DeleteCharacter();
                    return true;
                case Key.U:
                    // TODO: Implement undo
                    return true;
                case Key.R when modifiers == KeyModifiers.Control: // Ctrl+R
                    // TODO: Implement redo
                    return true;
                case Key.Oem1 when modifiers == KeyModifiers.Shift: // Shift+; (colon)
                    CurrentMode = VimMode.Command;
                    CommandBuffer = ":";
                    return true;
                case Key.Escape:
                    return true; // Stay in normal mode
            }
            
            return false;
        }

        private bool HandleInsertMode(Key key, KeyModifiers modifiers)
        {
            switch (key)
            {
                case Key.Escape:
                    CurrentMode = VimMode.Normal;
                    MoveCursor(-1, 0); // Move back one position like vim
                    return true;
            }
            
            return false; // Let normal text input through
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
            }
            
            // Add character to command buffer
            if (key >= Key.A && key <= Key.Z || key >= Key.D0 && key <= Key.D9 || key == Key.Space)
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

        private void ExecuteCommand()
        {
            var command = CommandBuffer.Trim();
            if (command.StartsWith(":"))
            {
                command = command[1..];
            }

            switch (command.ToLower())
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

        private void MoveCursor(int deltaX, int deltaY)
        {
            // TODO: Implement cursor movement logic
            // This would require access to the TextBox's selection/cursor position
        }

        private void MoveWordForward()
        {
            // TODO: Implement word forward movement
        }

        private void MoveWordBackward()
        {
            // TODO: Implement word backward movement
        }

        private void EnterInsertModeBelow()
        {
            // TODO: Implement insert mode below current line
            CurrentMode = VimMode.Insert;
        }

        private void EnterInsertModeAbove()
        {
            // TODO: Implement insert mode above current line
            CurrentMode = VimMode.Insert;
        }

        private void DeleteToLineEnd()
        {
            // TODO: Implement delete to line end
        }

        private void DeleteCharacter()
        {
            // TODO: Implement character deletion
        }

        private void StartDeleteCommand()
        {
            // TODO: Implement delete command (dd, dw, etc.)
        }

        private char? GetCharFromKey(Key key, KeyModifiers modifiers)
        {
            // Simple mapping - would need to be more comprehensive for real use
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
