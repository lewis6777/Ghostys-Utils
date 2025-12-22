using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Security.Cryptography;
using System.IO;
using System.Net.Http;

namespace Ghosty
{
    public class Uploader : EditorWindow
    {
        SettingsData settings;

        public string mapId = "";
        public string filePath = "";
        public string modioEndpoint = "";
        public string token = "";

        static string response = "";

        bool showToken = false;

        [MenuItem("Ghosty/Uploader", priority = 200)]
        public static void ShowWindow()
        {
            Uploader wnd = GetWindow<Uploader>();
            wnd.minSize = new Vector2(350, 350);
            wnd.titleContent = new GUIContent("Uploader");
        }

        void OnEnable()
        {
            settings = RunOnLoad.Settings;
            mapId = settings.mapId;
            filePath = settings.filePath;
            modioEndpoint = EditorPrefs.GetString("Uploader_modioEndpoint", "");
            token = EditorPrefs.GetString("Uploader_token", "");
        }

        void OnDisable()
        {
            settings = RunOnLoad.Settings;
            settings.mapId = mapId;
            settings.filePath = filePath;
            EditorPrefs.SetString("Uploader_modioEndpoint", modioEndpoint);
            EditorPrefs.SetString("Uploader_token", token);
        }

        public void OnGUI()
        {
            GUILayout.Space(8);
            GUILayout.Label("Documentation for using this is available on my website.");

            Color oldColor = GUI.color;
            GUI.backgroundColor = Color.lightBlue;
            if (GUILayout.Button("Documentation"))
                Application.OpenURL("https://ghostysstuff.com/maps/");
            GUI.backgroundColor = oldColor;

            GUILayout.Label("Vadix made the original Uploader logic :)", EditorStyles.miniLabel);

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Space(4);

            GUILayout.Label("Map ID:");
            mapId = GUILayout.TextField(mapId);

            GUILayout.Label("Zip file path:");

            EditorGUILayout.BeginHorizontal();

            GUI.SetNextControlName("ZipPathField");
            filePath = EditorGUILayout.TextField(filePath);

            if (GUILayout.Button("Browse", GUILayout.Width(65)))
            {
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                string exportsPath = Path.Combine(projectRoot, "Exports");
                string startDirectory = !string.IsNullOrEmpty(filePath)
                    ? Path.GetDirectoryName(filePath)
                    : Directory.Exists(exportsPath)
                        ? exportsPath
                        : projectRoot;

                string selected = EditorUtility.OpenFilePanel(
                    "Select Zip File",
                    startDirectory,
                    "zip"
                );
                if (!string.IsNullOrEmpty(selected))
                {
                    filePath = selected;
                    GUI.FocusControl(null);
                }
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Label("Mod.io API url:");
            modioEndpoint = EditorGUILayout.TextField(modioEndpoint);

            GUILayout.Label("Token:");
            GUILayout.BeginHorizontal();
            token = showToken
                ? EditorGUILayout.TextField(token)
                : EditorGUILayout.PasswordField(token);
            showToken = GUILayout.Toggle(showToken, "", GUILayout.Width(20));
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            if (settings != null)
            {
                settings.mapId = mapId;
                settings.filePath = filePath;
                EditorUtility.SetDirty(settings);
            }

            if (GUILayout.Button("Upload"))
                Upload();

            GUILayout.Label("Response:");
            EditorGUILayout.LabelField(response, EditorStyles.wordWrappedLabel);
        }

        static string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filename))
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        public static void Upload()
        {
            var settings = RunOnLoad.Settings;
            var mapId = settings.mapId;
            var filePath = settings.filePath;
            var modioEndpoint = EditorPrefs.GetString("Uploader_modioEndpoint", "");
            var token = EditorPrefs.GetString("Uploader_token", "");

            response = "Uploading...";
            foreach (var w in Resources.FindObjectsOfTypeAll<Uploader>())
                w.Repaint();

            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var hash = CalculateMD5(filePath);
                    var url = $"{modioEndpoint}/games/6657/mods/{mapId}/files";

                    using (HttpClient client = new HttpClient())
                    {
                        MultipartFormDataContent formContent = new MultipartFormDataContent();

                        var stream = File.OpenRead(filePath);
                        var fileContent = new StreamContent(stream);

                        formContent.Add(fileContent, "filedata", Path.GetFileName(filePath));
                        formContent.Add(new StringContent("true"), "active");
                        formContent.Add(new StringContent(hash), "filehash");

                        using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
                        {
                            request.Headers.Add("Authorization", "Bearer " + token);
                            request.Headers.Add("Accept", "application/json");
                            request.Content = formContent;

                            using (HttpResponseMessage resp = client.SendAsync(request).Result)
                            {
                                string result = resp.StatusCode + " " + resp.Content.ReadAsStringAsync().Result;

                                EditorApplication.delayCall += () =>
                                {
                                    response = result;
                                    Debug.Log(result);
                                    foreach (var w in Resources.FindObjectsOfTypeAll<Uploader>())
                                        w.Repaint();
                                };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    EditorApplication.delayCall += () =>
                    {
                        response = ex.Message;
                        Debug.LogException(ex);
                        foreach (var w in Resources.FindObjectsOfTypeAll<Uploader>())
                            w.Repaint();
                    };
                }
            });
        }


    }
}
