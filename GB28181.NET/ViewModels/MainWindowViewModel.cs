using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GB28181.Utilities;
using GB28181.Utilities.util;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GB28181.NET.ViewModels
{
    public partial class MainWindowViewModel : BaseViewModel
    {
        private SipUdpClient _udpClient;

        private string _fileExtension = "MP4 File|*.mp4";

        [ObservableProperty]
        private string _filePath = "";

        public MainWindowViewModel()
        {
            _udpClient = new SipUdpClient();
        }

        [RelayCommand]
        private void UploadSystemFile()
        {
            try
            {
                OpenFileDialog fileDialog = new()
                {
                    Filter = _fileExtension,
                    CheckFileExists = true,
                    CheckPathExists = true
                };

                if (!(fileDialog.ShowDialog() ?? false))
                {
                    App.Current.log.LogTrace("Close file dialog...");
                    return;
                }

                if (fileDialog.FileName.IsEmpty())
                {
                    throw new ArgumentNullException("Upload file path not be null!");
                }

                FilePath = fileDialog.FileName;
            }
            catch (Exception ex)
            {
                App.Current.log.LogError(ex.Message);
                Debug.WriteLine(ex.Message);
            }
        }

        [RelayCommand]
        private async Task PushVideoStreamAsync()
        {
            try
            {
                if (FilePath.IsEmpty())
                {
                    throw new ArgumentNullException("Upload file path not be null!");
                }

                string address = await IPAddressHelper.GetIPV4AdressAsync();
                Device device = Device.CreateDevice("34020000002110000005", "13579wmm", address, 50003);
                device.AddChannel("34020000002110000007", FilePath);
                _udpClient.AddDevice(device);
                _udpClient.Registry(device.Username, new IPEndPoint(IPAddress.Parse(address), 15060));
            }
            catch (Exception ex)
            {
                App.Current.log.LogError(ex.Message);
                Debug.WriteLine(ex.Message);
            }
            finally
            {
            }
        }
    }
}
