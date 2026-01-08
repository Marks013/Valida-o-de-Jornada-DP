using System;
using System.Reflection;

namespace ValidadorJornada.Core.Helpers
{
    public static class VersionInfo
    {
        private static string? _version;
        private static string? _buildDate;

        public static string Version
        {
            get
            {
                if (_version == null)
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    var version = assembly.GetName().Version;
                    _version = version != null 
                        ? $"{version.Major}.{version.Minor}.{version.Build}" 
                        : "1.0.0";
                }
                return _version;
            }
        }

        public static string BuildDate
        {
            get
            {
                if (_buildDate == null)
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                    _buildDate = attribute?.InformationalVersion ?? DateTime.Now.ToString("dd/MM/yyyy");
                }
                return _buildDate;
            }
        }

        public static string FullVersion => $"v{Version}";
        
        public static string FullVersionWithDate => $"v{Version} ({BuildDate})";
    }
}