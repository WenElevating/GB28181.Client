using Microsoft.Extensions.Logging;
using System.Configuration;
using System.Data;
using System.Windows;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "App.config", ConfigFileExtension = "config", Watch = true)]
namespace GB28181.NET
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddDebug());

        public ILogger log;

        public new static App Current = (App)Application.Current;

        public static log4net.ILog operationLog = log4net.LogManager.GetLogger("OperationLog");

        public static log4net.ILog errorLog = log4net.LogManager.GetLogger("ErrorLog");

        public App()
        {
            log = factory.CreateLogger<App>();
            operationLog.Info("启动程序...");

            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        /// <summary>
        /// Task相关全局异常处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            operationLog.Error("[Domain Exception]" + e.Exception);
            e.SetObserved();
        }

        /// <summary>
        /// Domain
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            errorLog.Error("[Domain Exception]: " + (Exception)e.ExceptionObject);
        }

        /// <summary>
        /// Dispatcher全局异常处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            operationLog.Error("[Dispatcher Exception]: " + e.Exception);
            e.Handled = true;
        }
    }

}
