using UnityEngine;

namespace Ghosty
{
    public class SettingsData : ScriptableObject
    {
        public bool openCodeStartup;

        public bool uploadOnExport;
        public bool askBeforeUpload;

        public string mapId = "";
        public string filePath = "";
    }
}
