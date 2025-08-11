using PSTARV2MonitoringApp.Models;
using PSTARV2MonitoringApp.ViewModels.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PSTARV2MonitoringApp.Views.Controls
{
    public partial class DeviceStatusCard : UserControl
    {
        public DeviceStatusCardViewModel ViewModel { get; }

        public DeviceStatusCard ()
        {
            InitializeComponent();
        }

        //여기로 ViewModel을 전달받아 초기화하는 생성자를 추가한다.
        public DeviceStatusCard(DeviceStatusCardViewModel viewModel, ObservableCollection<DeviceStatusCardModel> dataContext)
        {
            ViewModel = viewModel; // ViewModel을 초기화한다.
            //DataContext = this;
            DataContext = dataContext;
            InitializeComponent(); // XAML에 정의된 UI 요소들을 초기화 한다.
        }

        public DeviceStatusCard(DeviceStatusCardViewModel viewModel)
        {
            ViewModel = viewModel; // ViewModel을 초기화한다.
            DataContext = this;
            InitializeComponent(); // XAML에 정의된 UI 요소들을 초기화 한다.
        }
    }
}
