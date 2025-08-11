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

        // 버튼 클릭 이벤트 핸들러 추가
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.MessageBox.Show($"버튼 테스트", "오류", System.Windows.MessageBoxButton.OK, MessageBoxImage.Information);
                // 직접 ViewModel의 메서드 호출
                //DeviceStatusCardViewModel.UpdateRandomDeviceStatus();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"오류 발생: {ex.Message}", "오류", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
