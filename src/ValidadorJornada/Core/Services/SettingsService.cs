using System;
using System.IO;
using ValidadorJornada.Core.Helpers;

namespace ValidadorJornada.Core.Services
{
    public class UserSettings
    {
        public bool AutoFormatarHorarios { get; set; }
        public bool ValidarInterjornadaAtivo { get; set; }
    }

    public class SettingsService
    {
        private readonly string _settingsPath;
        private UserSettings? _cachedSettings;

        public SettingsService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appData, "ValidadorJornada");
            
            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);
                
            _settingsPath = Path.Combine(appFolder, "settings.json");
        }

        public UserSettings LoadSettings()
        {
            if (_cachedSettings != null)
                return _cachedSettings;

            // ✅ REFATORADO: Usa JsonFileHelper
            _cachedSettings = JsonFileHelper.Load<UserSettings>(_settingsPath) ?? new UserSettings();
            return _cachedSettings;
        }

        public void SaveSettings(UserSettings settings)
        {
            try
            {
                // ✅ REFATORADO: Usa JsonFileHelper
                JsonFileHelper.Save(_settingsPath, settings);
                _cachedSettings = settings;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Não foi possível salvar as configurações", ex);
            }
        }
    }
}
