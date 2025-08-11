using PSTARV2MonitoringApp.Models;
using PSTARV2MonitoringApp.ViewModels.Controls;
using PSTARV2MonitoringApp.ViewModels.Pages;
using System.Collections.Specialized;
using System.Windows.Controls;
using System.Windows.Markup.Localizer;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace PSTARV2MonitoringApp.Views.Pages
{
    public partial class DashboardPage : INavigableView<DashboardViewModel>
    {
        public DashboardViewModel ViewModel { get; }
        public DeviceStatusCardViewModel DeviceStatusCardViewModel { get; }

        public DashboardPage(DashboardViewModel viewModel, DeviceStatusCardViewModel deviceStatusCardViewModel)
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark); //Dark 테마로 실행

            ViewModel = viewModel;
            DeviceStatusCardViewModel = deviceStatusCardViewModel;
            DataContext = this;
            InitializeComponent();

            // 초기 데이터 설정
            DeviceStatusCardViewModel.InitializeDeviceStatusCardModels();

            // 컬렉션 변경 이벤트 구독
            //DeviceStatusCardViewModel.DeviceStatusCardModels.CollectionChanged += DeviceStatusCardModels_CollectionChanged;


            // 초기 UI 업데이트
            //UpdateAllDeviceCards();

        }

        //private void DeviceStatusCardModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        //{
        //    // 컬렉션이 변경되면 모든 카드 업데이트
        //    UpdateAllDeviceCards();
        //}

        //private void UpdateAllDeviceCards()
        //{
        //    // 각 ID에 해당하는 카드 업데이트
        //    foreach (var model in DeviceStatusCardViewModel.DeviceStatusCardModels)
        //    {
        //        switch (model.DeviceId)
        //        {
        //            case "ID1":
        //                UpdateDeviceCard(deviceCard1, model);
        //                break;
        //            case "ID2":
        //                UpdateDeviceCard(deviceCard2, model);
        //                break;
        //            case "ID3":
        //                UpdateDeviceCard(deviceCard3, model);
        //                break;
        //        }
        //    }
        //}

        //private void UpdateDeviceCard(Grid card, DeviceStatusCardModel model)
        //{
        //    // 카드가 표시되도록 설정
        //    card.Visibility = Visibility.Visible;
        //    card.DataContext = model;
        //}

        //private void UpdateDeviceCard(Controls.DeviceStatusCard card, DeviceStatusCardModel model)
        //{
        //    // 카드가 표시되도록 설정
        //    card.Visibility = Visibility.Visible;
        //    card.DataContext = model;
        //}

       
    }
}