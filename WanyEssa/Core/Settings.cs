using System;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace WanyEssa.Core
{
    public class Settings
    {
        // Graphics settings
        public int MsaaLevel { get; set; } = 4;
        public bool FsaEnabled { get; set; } = false;
        public int FpsLimit { get; set; } = 60;
        
        // Console settings
        public bool ConsoleEnabled { get; set; } = true;
        public char ConsoleToggleKey { get; set; } = '`';
        
        // Input settings
        public Keys SettingsToggleKey { get; set; } = Keys.Escape;
        
        // Game settings
        public bool DebugMode { get; set; } = false;
        public bool ShowFps { get; set; } = true;
        
        // Singleton instance
        private static Settings? _instance = null;
        
        private Settings() {}
        
        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Settings();
                }
                return _instance!;
            }
        }
        
        public void Save(string filePath = "settings.json")
        {
            // Simple JSON saving implementation
            // This would be replaced with proper JSON serialization in a real implementation
            string json = string.Format(
                "{{\"MsaaLevel\": {0},\"FsaEnabled\": {1},\"FpsLimit\": {2},\"ConsoleEnabled\": {3},\"ConsoleToggleKey\": \"{4}\",\"SettingsToggleKey\": \"{5}\",\"DebugMode\": {6},\"ShowFps\": {7}}}",
                MsaaLevel,
                FsaEnabled.ToString().ToLower(),
                FpsLimit,
                ConsoleEnabled.ToString().ToLower(),
                ConsoleToggleKey,
                SettingsToggleKey.ToString(),
                DebugMode.ToString().ToLower(),
                ShowFps.ToString().ToLower()
            );
            
            System.IO.File.WriteAllText(filePath, json);
        }
        
        public void Load(string filePath = "settings.json")
        {
            if (System.IO.File.Exists(filePath))
            {
                // Simple JSON loading implementation
                // This would be replaced with proper JSON serialization in a real implementation
                string json = System.IO.File.ReadAllText(filePath);
                // Parse json and set properties
            }
        }
    }
}