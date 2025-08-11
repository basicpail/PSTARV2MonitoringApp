using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace PSTARV2MonitoringApp.Views.Dialogs
{
    public partial class IdFilterDialog : ContentDialog
    {
        public string? SelectedId { get; private set; }
        public bool IsFilterCleared { get; private set; }

        public IdFilterDialog(IEnumerable<string> availableIds)
        {
            InitializeComponent();
            LoadAvailableIds(availableIds);
        }

        private void LoadAvailableIds(IEnumerable<string> availableIds)
        {
            // 기존 라디오 버튼들 제거 (All 제외)
            var buttonsToRemove = IdSelectionPanel.Children
                .OfType<RadioButton>()
                .Where(rb => rb != AllRadioButton)
                .ToList();

            foreach (var button in buttonsToRemove)
            {
                IdSelectionPanel.Children.Remove(button);
            }

            // 새로운 ID 라디오 버튼들 추가
            foreach (var id in availableIds.Distinct().OrderBy(x => x))
            {
                var radioButton = new RadioButton
                {
                    Content = id,
                    GroupName = "IdFilter",
                    FontSize = 14,
                    Margin = new Thickness(0, 5, 0, 0),
                    Tag = id
                };
                IdSelectionPanel.Children.Add(radioButton);
            }
        }

        private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var selectedRadioButton = IdSelectionPanel.Children
                .OfType<RadioButton>()
                .FirstOrDefault(rb => rb.IsChecked == true);

            if (selectedRadioButton?.Tag?.ToString() != null)
            {
                SelectedId = selectedRadioButton.Tag.ToString();
            }
            else
            {
                SelectedId = null; // 전체 보기
            }

            IsFilterCleared = false;
        }

        private void OnSecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            SelectedId = null;
            IsFilterCleared = false;
        }

        private void OnClearFilterClick(object sender, RoutedEventArgs e)
        {
            AllRadioButton.IsChecked = true;
            SelectedId = null;
            IsFilterCleared = true;
            Hide();
        }
    }
}