using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BookShuffler.Models;
using BookShuffler.ViewModels;

namespace BookShuffler.Views
{
    public class EntityView : UserControl
    {
        private TextBox _textBox;
        private TextBlock _textBlock;
        private Button _summaryButton;
        private TextBlock _textContent;
        private ItemsControl _entityContent;
        
        public EntityView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _textBox = this.FindControl<TextBox>("SummaryTextBox");
            _textBlock = this.FindControl<TextBlock>("SummaryTextBlock");
            _summaryButton = this.FindControl<Button>("SummaryEditButton");
            _textContent = this.FindControl<TextBlock>("TextContent");
            _entityContent = this.FindControl<ItemsControl>("EntityContent");
        }

        private void Border_OnPointerEnter(object? sender, PointerEventArgs e)
        {
            _summaryButton.IsVisible = true;
        }

        private void Border_OnPointerLeave(object? sender, PointerEventArgs e)
        {
            _summaryButton.IsVisible = false;
        }

        private void SummaryEditButton_OnClick(object? sender, RoutedEventArgs e)
        {
            _textBox.IsVisible = true;
            _textBlock.IsVisible = false;
        }

        private void SummaryTextBox_OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _textBox.IsVisible = false;
                _textBlock.IsVisible = true;
            }
        }

        private void StyledElement_OnDataContextChanged(object? sender, EventArgs e)
        {
            if (this.DataContext is IndexCardViewModel)
            {
                this._entityContent.IsVisible = false;
                this._textContent.IsVisible = true;
                return;
            }

            if (this.DataContext is SectionViewModel)
            {
                this._entityContent.IsVisible = true;
                this._textContent.IsVisible = false;
                return;
            }
        }
    }
}