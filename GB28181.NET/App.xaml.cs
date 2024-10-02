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
        }
    }

}
