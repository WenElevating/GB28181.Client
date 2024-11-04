using GB28181.NET.ViewModels;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GB28181.NET.Views
{
    /// <summary>
    /// DeviceListPage.xaml 的交互逻辑
    /// </summary>
    public partial class DeviceListPage : Page
    {
        private DeviceListPageViewModel viewModel;

        public DeviceListPage()
        {
            InitializeComponent();
            viewModel = new DeviceListPageViewModel();
            DataContext = viewModel;
        }
    }
}
