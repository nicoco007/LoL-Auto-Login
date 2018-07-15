// Copyright © 2015-2018 Nicolas Gnyra

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
using System.Windows.Forms;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace LoLAutoLogin
{
    internal static class Config
    {
        private static YamlNode root;

        internal static void Load()
        {
            // get config directory & settings file
            var settingsFile = Path.Combine(Folders.Configuration, "LoLAutoLoginSettings.yaml");

            // make sure the config directory exists
            if (!Directory.Exists(Folders.Configuration))
                Directory.CreateDirectory(Folders.Configuration); // TODO: try/catch
            
            Logger.Info($"Loading settings from \"{settingsFile}\"");

            // check if settings exist
            if (File.Exists(settingsFile))
            {
                try
                {
                    // load settings from yaml
                    root = Util.ReadYaml<YamlMappingNode>(settingsFile);
                }
                catch (SyntaxErrorException ex)
                {
                    Logger.Warn("Failed to read settings YAML.");
                    Logger.PrintException(ex);
                    MessageBox.Show($"Failed to load settings from \"{settingsFile}\". Please check your syntax and try again.", "LoL Auto Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to read settings YAML.");
                    Logger.PrintException(ex);
                    MessageBox.Show($"Failed to load settings from \"{settingsFile}\".", "LoL Auto Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                Logger.Info("Settings file does not exist.");
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
    }
}
