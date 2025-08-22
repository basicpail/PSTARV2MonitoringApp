using PSTARV2MonitoringApp.ViewModels.Pages;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using Wpf.Ui.Abstractions.Controls;

namespace PSTARV2MonitoringApp.Views.Pages
{
    /// <summary>
    /// HelpPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class HelpPage : INavigableView<HelpViewModel>
    {
        public HelpViewModel ViewModel { get; }
        public HelpPage(HelpViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}
