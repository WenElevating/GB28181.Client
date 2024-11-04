using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GB28181.Utilities.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace GB28181.NET.ViewModels
{
    public partial class DeviceListPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Device>? deviceList;

        [ObservableProperty]
        private ICollectionView deviceView;

        private string filterText = string.Empty;
        public string FilterText
        {
            get => filterText;
            set
            {
                filterText = value;
                SetProperty(ref filterText, value);
                FilterTableData();
            }
        }

        public DeviceListPageViewModel()
        {
            Device device = Device.CreateDevice("34020000002110000005", "13579wmm", "192.168.1.100", 50003);
            Device device2 = Device.CreateDevice("34020000002110000009", "13579wmm", "192.168.1.110", 50009);
            //device.AddChannel("34020000002110000007", "1");
            deviceList = new ObservableCollection<Device>()
            {
                device,
                device2
            };
            deviceView = CollectionViewSource.GetDefaultView(deviceList);
        }

        private void FilterTableData()
        {
            if (DeviceView != null && DeviceView.CanFilter)
            {
                DeviceView.Filter = (item) =>
                {
                    if (string.IsNullOrEmpty(FilterText))
                    {
                        return true;
                    }
                    else
                    {
                        var model = item as Device;
                        return model?.Username.Contains(FilterText) ?? false;
                    }
                };
                DeviceView.Refresh();
            }
        }
    }
}
