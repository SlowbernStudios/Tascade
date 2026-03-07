using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Tascade.Models;

namespace Tascade.Services
{
    public class FileOperationsService
    {
        private readonly Window _window;
        private readonly List<string> _recentFiles = new();

        public event Action<string>? FileOpened;
        public event Action<string>? FileSaved;
        public event Action<List<string>>? RecentFilesUpdated;

        public FileOperationsService(Window window)
        {
            _window = window;
        }

        public async Task<string> OpenFileDialogAsync()
        {
            var files = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Text/Markdown")
                    {
                        Patterns = new[] { "*.txt", "*.md", "*.markdown", "*.html" }
                    }
                }
            });

            return files.Count > 0 ? files[0].Path.LocalPath : string.Empty;
        }

        public async Task<string> SaveFileDialogAsync(string defaultFileName)
        {
            var file = await _window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                SuggestedFileName = defaultFileName,
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Text") { Patterns = new[] { "*.txt" } },
                    new FilePickerFileType("Markdown") { Patterns = new[] { "*.md" } }
                }
            });

            return file?.Path.LocalPath ?? string.Empty;
        }

        public async Task<NotepadData?> LoadNotepadAsync(string filePath)
        {
            try
            {
                var content = await File.ReadAllTextAsync(filePath);
                var notepad = new NotepadData
                {
                    Title = Path.GetFileNameWithoutExtension(filePath),
                    Content = new RichTextContent { MarkdownContent = content },
                    FilePath = filePath
                };

                AddRecent(filePath);
                FileOpened?.Invoke(filePath);
                return notepad;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> SaveNotepadAsync(string filePath, NotepadData notepad)
        {
            try
            {
                await File.WriteAllTextAsync(filePath, notepad.Content.MarkdownContent ?? string.Empty);
                AddRecent(filePath);
                FileSaved?.Invoke(filePath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void LoadRecentFiles(string recentFilesPath)
        {
            try
            {
                if (File.Exists(recentFilesPath))
                {
                    var json = File.ReadAllText(recentFilesPath);
                    var entries = JsonSerializer.Deserialize<List<string>>(json);
                    _recentFiles.Clear();
                    if (entries != null)
                    {
                        _recentFiles.AddRange(entries.Where(File.Exists));
                    }
                }

                RecentFilesUpdated?.Invoke(new List<string>(_recentFiles));
            }
            catch
            {
                _recentFiles.Clear();
                RecentFilesUpdated?.Invoke(new List<string>(_recentFiles));
            }
        }

        public void SaveRecentFiles(string recentFilesPath)
        {
            try
            {
                var dir = Path.GetDirectoryName(recentFilesPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var json = JsonSerializer.Serialize(_recentFiles, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(recentFilesPath, json);
            }
            catch
            {
            }
        }

        private void AddRecent(string filePath)
        {
            _recentFiles.RemoveAll(path => string.Equals(path, filePath, StringComparison.OrdinalIgnoreCase));
            _recentFiles.Insert(0, filePath);

            if (_recentFiles.Count > 10)
            {
                _recentFiles.RemoveRange(10, _recentFiles.Count - 10);
            }

            RecentFilesUpdated?.Invoke(new List<string>(_recentFiles));
        }
    }
}


