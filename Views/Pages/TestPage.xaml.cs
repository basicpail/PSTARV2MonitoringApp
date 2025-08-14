using PSTARV2MonitoringApp.ViewModels.Pages;
using PSTARV2MonitoringApp.ViewModels.Controls;
using Wpf.Ui.Abstractions.Controls;

namespace PSTARV2MonitoringApp.Views.Pages
{
    public partial class TestPage : INavigableView<TestViewModel>
    {
        public TestViewModel ViewModel { get; }
        public PSTARDevicePanelViewModel DevicePanelViewModel { get; }

        public TestPage(TestViewModel viewModel, PSTARDevicePanelViewModel devicePanelViewModel)
        {
            ViewModel = viewModel;
            DevicePanelViewModel = devicePanelViewModel;
            DataContext = this;

            InitializeComponent();

            // ViewModel에 이 페이지의 참조 전달
            ViewModel.SetTestPage(this);
        }
    }
}