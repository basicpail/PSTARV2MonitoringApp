using PSTARV2MonitoringApp.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace PSTARV2MonitoringApp.Views.Pages
{
    public partial class DashboardPage : INavigableView<DashboardViewModel>
    {
        public DashboardViewModel ViewModel { get; }

        public DashboardPage(DashboardViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}
