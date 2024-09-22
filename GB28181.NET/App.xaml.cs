using Microsoft.Extensions.Logging;
using System.Configuration;
using System.Data;
using System.Windows;

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

        public App()
        {
            log = factory.CreateLogger<App>();
        }
    }

}
