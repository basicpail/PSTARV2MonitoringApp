using PSTARV2MonitoringApp.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace PSTARV2MonitoringApp.Views.Pages
{
    public partial class TestPage : INavigableView<TestViewModel>
    {
        public TestViewModel ViewModel { get; }

        public TestPage(TestViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}
