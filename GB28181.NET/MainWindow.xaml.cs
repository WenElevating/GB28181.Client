using GB28181.NET.Enums;
using GB28181.NET.ViewModels;
using GB28181.NET.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GB28181.NET
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly BaseViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;
            FrameWork.Content = new Frame()
            {
                Content = new MainPage()
            };
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn)
            {
                return;
            }

            object? view = null;

            switch (btn.Tag.ToString())
            {
                case "Home":
                    view = App.Current.serviceProvider.GetRequiredService<MainPage>();
                    break;
                case "Setting":
                    view = App.Current.serviceProvider.GetRequiredService<SipSettingPage>();
                    break;
                case "Device":
                    view = App.Current.serviceProvider.GetRequiredService<DeviceListPage>();
                    break;
            }

            FrameWork.Navigate(view);
        }

        /// <summary>
        /// 侧边栏导航选中事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            DoubleAnimation widthAnmiation = new DoubleAnimation()
            {
                From = 55,
                To = 150,
                Duration = TimeSpan.FromSeconds(0.2),
                AutoReverse = false,
            };
            
            LeftRail.BeginAnimation(WidthProperty, widthAnmiation);
        }

        /// <summary>
        /// 侧边栏导航取消选中事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            DoubleAnimation widthAnmiation = new DoubleAnimation()
            {
                From = 150,
                To = 55,
                Duration = TimeSpan.FromSeconds(0.2),
                AutoReverse = false,
            };

            LeftRail.BeginAnimation(WidthProperty, widthAnmiation);
        }
    }
}