using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PSTARV2MonitoringApp.ViewModels.Dialogs
{
    public partial class IdFilterDialogViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _selectedIdFilter;

        [ObservableProperty]
        private bool _isFilterApplied = false;

        // 사용 가능한 ID 목록
        public ObservableCollection<string> AvailableIds { get; } = new ObservableCollection<string>();

        // ID 필터 적용 이벤트
        public event Action<string> FilterApplied;

        public IdFilterDialogViewModel()
        {

        }

        // 사용 가능한 ID 목록 업데이트
        public void UpdateAvailableIds(IEnumerable<string> ids)
        {
            AvailableIds.Clear();
            foreach (var id in ids.Distinct().OrderBy(x => x))
            {
                AvailableIds.Add(id);
            }
        }

        [RelayCommand]
        public void ApplyFilter(string selectedId)
        {
            SelectedIdFilter = selectedId;
            IsFilterApplied = !string.IsNullOrEmpty(selectedId);
            FilterApplied?.Invoke(selectedId);
        }

        [RelayCommand]
        public void ClearFilter()
        {
            SelectedIdFilter = null;
            IsFilterApplied = false;
            FilterApplied?.Invoke(null);
        }
    }
}