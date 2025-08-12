using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PSTARV2MonitoringApp.ViewModels.Dialogs;
using Wpf.Ui.Controls;

namespace PSTARV2MonitoringApp.Views.Dialogs    
{
    public partial class IdFilterDialog : UserControl
    {
        private readonly IdFilterDialogViewModel _viewModel;
        
        public string SelectedId { get; private set; }
        public bool FilterCleared { get; private set; }

        public IdFilterDialog()
        {
            _viewModel = new IdFilterDialogViewModel();
            DataContext = _viewModel;
            InitializeComponent();
        }

        public void LoadAvailableIds(IEnumerable<string> availableIds)
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

            // ViewModel에 ID 목록 업데이트
            _viewModel.UpdateAvailableIds(availableIds);

            // 새로운 ID 라디오 버튼들 추가
            foreach (var id in availableIds.Distinct().OrderBy(x => x))
            {
                var radioButton = new RadioButton
                {
                    Content = id,
                    GroupName = "IdFilter",
                    FontSize = 14,
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 5, 0, 0),
                    Tag = id
                };
                
                // 현재 선택된 필터와 일치하면 선택 상태로 설정
                if (id == _viewModel.SelectedIdFilter)
                {
                    radioButton.IsChecked = true;
                    AllRadioButton.IsChecked = false;
                }
                
                IdSelectionPanel.Children.Add(radioButton);
            }
        }

        public void SetSelectedId(string id)
        {
            _viewModel.SelectedIdFilter = id;
            
            // 라디오 버튼 선택 상태 업데이트
            if (string.IsNullOrEmpty(id))
            {
                AllRadioButton.IsChecked = true;
            }
            else
            {
                AllRadioButton.IsChecked = false;
                
                foreach (var radioButton in IdSelectionPanel.Children.OfType<RadioButton>())
                {
                    if (radioButton.Tag?.ToString() == id)
                    {
                        radioButton.IsChecked = true;
                        break;
                    }
                }
            }
        }
    }
}