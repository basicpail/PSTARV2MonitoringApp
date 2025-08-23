using System;
using System.Windows;
using System.Windows.Controls;

namespace PSTARV2MonitoringApp.Views.Pages
{
    public partial class DashboardPage2 : Page
    {
        double DataGridHeightRatio = 0.67;
        public Dashboard2ViewModel ViewModel { get; }

        public DashboardPage2(Dashboard2ViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();

        }

        private void DockPanelSizeChanged(object sender, SizeChangedEventArgs e)
        {

            ApplyDataGridMaxHeight(e.NewSize.Width, e.NewSize.Height);
        }

        private void ApplyDataGridMaxHeight(double W, double H)
        {
            //DataGrid의 최대 높이 제한
            double maxHeight = H * DataGridHeightRatio;
            SituationInfoDataGrid.MaxHeight = Math.Truncate(maxHeight);
            RawDataDataGrid.MaxHeight = Math.Truncate(maxHeight);
        }
    }
}
