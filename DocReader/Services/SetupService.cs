using DocReader.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace DocReader.Services
{
    public class SetupService
    {
        private const string SettingsKey = "CameraAutoMode";

        public SetupService()
        {

        }

        public async Task<bool> GetAutomode()
        {
            bool automode = await ApplicationData.Current.LocalSettings.ReadAsync<bool>(SettingsKey);

            return automode;   
        }

        public async Task SetAutomode(bool isAutoCapture)
        {
            await ApplicationData.Current.LocalSettings.SaveAsync<bool>(SettingsKey,isAutoCapture);
        }
    }
}
