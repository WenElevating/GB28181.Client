using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GB28181.NET.Model;
using GB28181.NET.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GB28181.NET.ViewModels
{
    public enum DataSource
    {
        DataBase,
        JsonFile
    }

    public partial class SipSettingPageViewModel : ObservableObject
    {
        private readonly DataSource dataSource;

        private readonly string sipServerSettingPath = "SipServerSetting.json";

        private readonly string mediaServerSettingPath = "mediaServerSetting.json";

        [ObservableProperty]
        private SipServerSetting? sipSetting;

        [ObservableProperty]
        private MediaServerSetting? mediaSetting;

        public IRelayCommand SaveBasicSettingCommand { get; set; }

        public SipSettingPageViewModel()
        {
            // todo 读取系统配置
            dataSource = DataSource.JsonFile;
            sipServerSettingPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, sipServerSettingPath);
            mediaServerSettingPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, mediaServerSettingPath);
            SaveBasicSettingCommand = new AsyncRelayCommand(SaveSettingAsync);
            InitSetting();
        }

        private async void InitSetting()
        {
            try
            {
                if (dataSource == DataSource.JsonFile)
                {
                    SipSetting = await JsonUtil.ReadJsonFileAsync<SipServerSetting>(sipServerSettingPath) ?? new();
                    MediaSetting = await JsonUtil.ReadJsonFileAsync<MediaServerSetting>(mediaServerSettingPath) ?? new();
                    return;
                }

                //TODO  数据库查询
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                App.errorLog.Error(ex);
            }
        }

        /// <summary>
        /// 保存配置信息
        /// </summary>
        /// <returns></returns>
        private async Task SaveSettingAsync()
        {
            try
            {
                if (dataSource == DataSource.JsonFile)
                {
                    await JsonUtil.WriteJsonFileAsync(mediaServerSettingPath, MediaSetting);
                    await JsonUtil.WriteJsonFileAsync(sipServerSettingPath, SipSetting);
                    MessageBox.Show("保存成功！");
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败！" + ex.Message);
            }
        }
    }
}
