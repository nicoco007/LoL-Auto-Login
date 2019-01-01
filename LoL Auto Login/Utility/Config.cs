// Copyright © 2015-2019 Nicolas Gnyra

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.

// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace LoLAutoLogin
{
    internal static class Config
    {
        private static YamlMappingNode root;
        private static readonly string SettingsFile = Path.Combine(Folders.Configuration, "LoLAutoLoginSettings.yaml");

        internal static void Load()
        {
            // make sure the config directory exists
            if (!Directory.Exists(Folders.Configuration))
                Directory.CreateDirectory(Folders.Configuration); // TODO: try/catch

            Logger.Info($"Loading settings from \"{SettingsFile}\"");

            // check if settings exist
            if (File.Exists(SettingsFile))
            {
                try
                {
                    // load settings from yaml
                    root = Util.ReadYaml<YamlMappingNode>(SettingsFile);
                }
                catch (SyntaxErrorException ex)
                {
                    Logger.PrintException("Syntax error occured while reading settings YAML", ex);
                    MessageBox.Show($"Failed to load settings from \"{SettingsFile}\". Please check your syntax and try again.", "LoL Auto Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    Logger.PrintException("Failed to read settings YAML", ex);
                    MessageBox.Show($"Failed to load settings from \"{SettingsFile}\".", "LoL Auto Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                Logger.Info("Settings file does not exist.");
            }
        }

        internal static void Save()
        {
            Logger.Info($"Saving settings to \"{SettingsFile}\"");

            try
            {
                Util.WriteYaml(SettingsFile, root);
            }
            catch (Exception ex)
            {
                Logger.PrintException("Failed to write settings YAML.", ex);
            }
        }

        internal static string GetStringValue(string key, string defaultValue)
        {
            var node = GetNodeByPath(key);

            if (node == null || !(node is YamlScalarNode))
                return defaultValue;

            return (node as YamlScalarNode).Value;
        }

        internal static byte GetByteValue(string key, byte defaultValue)
        {
            var node = GetNodeByPath(key);

            if (node == null || !(node is YamlScalarNode))
                return defaultValue;

            string strValue = (node as YamlScalarNode).Value;
            byte value = defaultValue;

            if (!byte.TryParse(strValue, out value))
                Logger.Warn("Failed to parse \"{0}\" as byte", strValue);

            return value;
        }

        internal static int GetIntegerValue(string key, int defaultValue)
        {
            var node = GetNodeByPath(key);

            if (node == null || !(node is YamlScalarNode))
                return defaultValue;

            string strValue = (node as YamlScalarNode).Value;
            int value = defaultValue;

            if (!int.TryParse(strValue, out value))
                Logger.Warn("Failed to parse \"{0}\" as integer", strValue);

            return value;
        }

        internal static float GetFloatValue(string key, float defaultValue)
        {
            var node = GetNodeByPath(key);

            if (node == null || !(node is YamlScalarNode))
                return defaultValue;

            string strValue = (node as YamlScalarNode).Value;
            float value = defaultValue;

            if (!float.TryParse(strValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                Logger.Warn("Failed to parse \"{0}\" as float", strValue);

            return value;
        }

        internal static bool GetBooleanValue(string key, bool defaultValue)
        {
            var node = GetNodeByPath(key);

            if (node == null || !(node is YamlScalarNode))
                return defaultValue;

            string strValue = (node as YamlScalarNode).Value;
            bool value = defaultValue;

            if (!bool.TryParse(strValue, out value))
                Logger.Warn("Failed to parse \"{0}\" as boolean", strValue);

            return value;
        }

        internal static void SetValue(string key, object value)
        {
            SetNodeByPath(key, value);
            Save();
        }

        private static YamlNode GetNodeByPath(string key)
        {
            Logger.Trace($"Getting key \"{key}\"");

            string[] parts = key.Split('.');
            YamlNode currentNode = root;

            for (int i = 0; i < parts.Length; i++)
            {
                if (currentNode != null && currentNode is YamlMappingNode && (currentNode as YamlMappingNode).Children.ContainsKey(parts[i]))
                    currentNode = currentNode[parts[i]];
                else
                    break;
            }

            if (currentNode == null)
                Logger.Trace("Failed to find node with path " + key);

            return currentNode;
        }

        private static void SetNodeByPath(string key, object value)
        {
            Logger.Trace($"Setting key \"{key}\" to \"{value}\"");

            string[] parts = key.Split('.');
            YamlNode currentNode = root;

            for (int i = 0; i < parts.Length; i++)
            {
                if (!(currentNode is YamlMappingNode))
                {
                    Logger.Error("{0} is not a YamlMappingNode", string.Join(".", parts.Take(i + 1)));
                    return;
                }

                var mappingNode = currentNode as YamlMappingNode;

                if (!mappingNode.Children.ContainsKey(parts[i]))
                {
                    if (i == parts.Length - 1)
                        mappingNode.Children.Add(new YamlScalarNode(parts[i]), new YamlScalarNode());
                    else
                        mappingNode.Children.Add(new YamlScalarNode(parts[i]), new YamlMappingNode());
                }

                if (i == parts.Length - 1)
                    (currentNode[parts[i]] as YamlScalarNode).Value = value.ToString();
                else
                    currentNode = currentNode[parts[i]];
            }
        }
    }
}
