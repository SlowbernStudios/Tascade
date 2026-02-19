using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Tascade.Models;

namespace Tascade.Controls
{
    public partial class RichTextEditor : UserControl
    {
        private RichTextContent _content;

        public RichTextEditor()
        {
            InitializeComponent();
            _content = new RichTextContent();
            DataContext = _content;
            
            // Hook up toolbar events
            SetupToolbarEvents();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void SetupToolbarEvents()
        {
            // Bold button
            var boldButton = this.FindControl<Button>("BoldButton");
            if (boldButton != null)
                boldButton.Click += (s, e) => ToggleFormatting("b");

            // Italic button
            var italicButton = this.FindControl<Button>("ItalicButton");
            if (italicButton != null)
                italicButton.Click += (s, e) => ToggleFormatting("i");

            // Underline button
            var underlineButton = this.FindControl<Button>("UnderlineButton");
            if (underlineButton != null)
                underlineButton.Click += (s, e) => ToggleFormatting("u");

            // Bullet list button
            var bulletListButton = this.FindControl<Button>("BulletListButton");
            if (bulletListButton != null)
                bulletListButton.Click += (s, e) => ToggleList("ul");

            // Number list button
            var numberListButton = this.FindControl<Button>("NumberListButton");
            if (numberListButton != null)
                numberListButton.Click += (s, e) => ToggleList("ol");

            // Link button
            var linkButton = this.FindControl<Button>("LinkButton");
            if (linkButton != null)
                linkButton.Click += (s, e) => InsertLink();

            // Table button
            var tableButton = this.FindControl<Button>("TableButton");
            if (tableButton != null)
                tableButton.Click += (s, e) => InsertTable();

            // Heading dropdown
            var headingComboBox = this.FindControl<ComboBox>("HeadingComboBox");
            if (headingComboBox != null)
                headingComboBox.SelectionChanged += (s, e) => ApplyHeading();
        }

        private void ToggleFormatting(string tag)
        {
            var textBox = this.FindControl<TextBox>("ContentTextBox");
            if (textBox == null) return;
            
            var selectedText = textBox.SelectedText;
            
            if (!string.IsNullOrEmpty(selectedText))
            {
                var formattedText = $"<{tag}>{selectedText}</{tag}>";
                var currentText = textBox.Text ?? "";
                var start = textBox.SelectionStart;
                var end = start + selectedText.Length;
                
                textBox.Text = currentText.Substring(0, start) + formattedText + currentText.Substring(end);
                UpdateContent();
            }
        }

        private void ToggleList(string listType)
        {
            var textBox = this.FindControl<TextBox>("ContentTextBox");
            if (textBox == null) return;
            
            var selectedText = textBox.SelectedText;
            
            if (!string.IsNullOrEmpty(selectedText))
            {
                var lines = selectedText.Split('\n');
                var listItems = lines.Select(line => $"<li>{line}</li>");
                var listContent = string.Join("\n", listItems);
                var formattedText = $"<{listType}>\n{listContent}\n</{listType}>";
                
                var currentText = textBox.Text ?? "";
                var start = textBox.SelectionStart;
                var end = start + selectedText.Length;
                
                textBox.Text = currentText.Substring(0, start) + formattedText + currentText.Substring(end);
                UpdateContent();
            }
        }

        private void InsertLink()
        {
            // For now, insert a simple link template
            var textBox = this.FindControl<TextBox>("ContentTextBox");
            if (textBox == null) return;
            
            var selectedText = textBox.SelectedText;
            var linkText = string.IsNullOrEmpty(selectedText) ? "Link Text" : selectedText;
            var linkHtml = $"<a href=\"#\">{linkText}</a>";
            
            var currentText = textBox.Text ?? "";
            var start = textBox.SelectionStart;
            var end = start + selectedText.Length;
            
            textBox.Text = currentText.Substring(0, start) + linkHtml + currentText.Substring(end);
            UpdateContent();
        }

        private void InsertTable()
        {
            // Insert a simple 2x2 table template
            var tableHtml = @"<table border=""1"">
  <tr>
    <td>Cell 1</td>
    <td>Cell 2</td>
  </tr>
  <tr>
    <td>Cell 3</td>
    <td>Cell 4</td>
  </tr>
</table>";

            var textBox = this.FindControl<TextBox>("ContentTextBox");
            if (textBox == null) return;
            
            var currentText = textBox.Text ?? "";
            var start = textBox.SelectionStart;
            
            textBox.Text = currentText.Substring(0, start) + tableHtml + currentText.Substring(start);
            UpdateContent();
        }

        private void ApplyHeading()
        {
            var headingComboBox = this.FindControl<ComboBox>("HeadingComboBox");
            var textBox = this.FindControl<TextBox>("ContentTextBox");
            if (headingComboBox == null || textBox == null) return;
            
            var selectedText = textBox.SelectedText;
            
            if (!string.IsNullOrEmpty(selectedText) && headingComboBox.SelectedIndex >= 0)
            {
                var headingTags = new[] { "h1", "h2", "h3", "h4", "h5", "h6", "p" };
                var selectedTag = headingTags[headingComboBox.SelectedIndex];
                var formattedText = $"<{selectedTag}>{selectedText}</{selectedTag}>";
                
                var currentText = textBox.Text ?? "";
                var start = textBox.SelectionStart;
                var end = start + selectedText.Length;
                
                textBox.Text = currentText.Substring(0, start) + formattedText + currentText.Substring(end);
                UpdateContent();
            }
        }

        private void UpdateContent()
        {
            var textBox = this.FindControl<TextBox>("ContentTextBox");
            if (textBox == null) return;
            
            _content.HtmlContent = textBox.Text ?? "";
            
            // Extract plain text for search/export
            _content.PlainText = System.Text.RegularExpressions.Regex.Replace(_content.HtmlContent, "<[^>]*>", "");
            _content.LastModified = DateTime.Now;
        }

        public new RichTextContent Content
        {
            get => _content;
            set
            {
                _content = value;
                DataContext = _content;
                
                var textBox = this.FindControl<TextBox>("ContentTextBox");
                if (textBox != null)
                {
                    textBox.Text = _content.HtmlContent;
                }
            }
        }
    }
}
