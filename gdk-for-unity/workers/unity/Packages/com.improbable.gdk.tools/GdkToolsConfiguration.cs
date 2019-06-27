﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEditor;
using UnityEngine;

namespace Improbable.Gdk.Tools
{
    [Serializable]
    public class GdkToolsConfiguration
    {
        public string SchemaStdLibDir;
        public List<string> SchemaSourceDirs = new List<string>();
        public string CodegenOutputDir;
        public string DescriptorOutputDir;
        public string DevAuthTokenDir;
        public int DevAuthTokenLifetimeDays;
        public bool SaveDevAuthTokenToFile;

        internal string RuntimeIpEditorPrefKey = "RuntimeIp";
        public string RuntimeIp => EditorPrefs.GetString(RuntimeIpEditorPrefKey);

        public string DevAuthTokenFullDir => Path.Combine(Application.dataPath, DevAuthTokenDir);
        public string DevAuthTokenFilepath => Path.Combine(DevAuthTokenFullDir, "DevAuthToken.txt");
        public int DevAuthTokenLifetimeHours => TimeSpan.FromDays(DevAuthTokenLifetimeDays).Hours;

        private static readonly string JsonFilePath = Path.GetFullPath("Assets/Config/GdkToolsConfiguration.json");

        private GdkToolsConfiguration()
        {
            ResetToDefault();
        }

        public void Save()
        {
            var json = JsonUtility.ToJson(this, true);
            File.WriteAllText(JsonFilePath, json);
        }

        internal List<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrEmpty(SchemaStdLibDir))
            {
                errors.Add($"{GdkToolsConfigurationWindow.SchemaStdLibDirLabel} cannot be empty.");
            }

            if (string.IsNullOrEmpty(CodegenOutputDir))
            {
                errors.Add($"{GdkToolsConfigurationWindow.CodegenOutputDirLabel} cannot be empty.");
            }

            if (string.IsNullOrEmpty(DescriptorOutputDir))
            {
                errors.Add($"{GdkToolsConfigurationWindow.DescriptorOutputDirLabel} cannot be empty.");
            }

            for (var i = 0; i < SchemaSourceDirs.Count; i++)
            {
                var schemaSourceDir = SchemaSourceDirs[i];

                if (string.IsNullOrEmpty(schemaSourceDir))
                {
                    errors.Add($"Schema path [{i}] is empty. You must provide a valid path.");
                    continue;
                }

                try
                {
                    var fullSchemaSourceDirPath = Path.Combine(Application.dataPath, "..", schemaSourceDir);
                    if (!Directory.Exists(fullSchemaSourceDirPath))
                    {
                        errors.Add($"{fullSchemaSourceDirPath} cannot be found.");
                    }
                }
                catch (ArgumentException)
                {
                    errors.Add($"Schema path [{i}] contains one or more invalid characters.");
                }
            }

            if (!string.IsNullOrEmpty(RuntimeIp) && !IPAddress.TryParse(RuntimeIp, out _))
            {
                errors.Add($"Runtime IP \"{RuntimeIp}\" is not a valid IP address.");
            }

            if (!SaveDevAuthTokenToFile)
            {
                return errors;
            }

            if (string.IsNullOrEmpty(DevAuthTokenDir))
            {
                errors.Add($"{GdkToolsConfigurationWindow.DevAuthTokenDirLabel} cannot be empty.");
            }
            else if (!DevAuthTokenDir.Equals("Resources") && !DevAuthTokenDir.EndsWith("/Resources"))
            {
                errors.Add(
                    $"{GdkToolsConfigurationWindow.DevAuthTokenDirLabel} must be at root of a Resources folder.");
            }

            return errors;
        }

        internal void ResetToDefault()
        {
            SchemaStdLibDir = DefaultValues.SchemaStdLibDir;
            CodegenOutputDir = DefaultValues.CodegenOutputDir;
            DescriptorOutputDir = DefaultValues.DescriptorOutputDir;
            DevAuthTokenDir = DefaultValues.DevAuthTokenDir;
            DevAuthTokenLifetimeDays = DefaultValues.DevAuthTokenLifetimeDays;

            SchemaSourceDirs.Clear();
            SchemaSourceDirs.Add(DefaultValues.SchemaSourceDir);
        }

        public static GdkToolsConfiguration GetOrCreateInstance()
        {
            return File.Exists(JsonFilePath) ? LoadFromFile() : CreateInstance();
        }

        private static GdkToolsConfiguration LoadFromFile()
        {
            return JsonUtility.FromJson<GdkToolsConfiguration>(File.ReadAllText(JsonFilePath));
        }

        private static GdkToolsConfiguration CreateInstance()
        {
            var config = new GdkToolsConfiguration();

            File.WriteAllText(JsonFilePath, JsonUtility.ToJson(config, true));

            return config;
        }

        private static class DefaultValues
        {
            public const string SchemaStdLibDir = "../../build/dependencies/schema/standard_library";
            public const string CodegenOutputDir = "Assets/Generated/Source";
            public const string DescriptorOutputDir = "../../build/assembly/schema";
            public const string SchemaSourceDir = "../../schema";
            public const string DevAuthTokenDir = "Resources";
            public const int DevAuthTokenLifetimeDays = 30;
        }
    }
}
