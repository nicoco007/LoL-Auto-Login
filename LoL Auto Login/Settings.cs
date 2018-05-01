﻿// Copyright © 2015-2018 Nicolas Gnyra

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
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace LoLAutoLogin
{
    internal static class Settings
    {
        internal static float LogoMatchTolerance { get; private set; }
        internal static float PasswordMatchTolerance { get; private set; }
        internal static bool ClientDetectionDebug { get; private set; }
        internal static int ClientTimeout { get; private set; }

        internal static string DebugDirectory { get { return Path.Combine(Directory.GetCurrentDirectory(), "debug"); } }

        internal static void Load()
        {
            // get config directory & settings file
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "Config");
            var settingsFile = Path.Combine(dir, "LoLAutoLoginSettings.yaml");

            // make sure the config directory exists
            Directory.CreateDirectory(dir);
            
            Logger.Info($"Loading settings from \"{settingsFile}\"");

            // define default settings
            var defaultSettings = new YamlMappingNode(
                new YamlScalarNode("login-detection"),
                new YamlMappingNode(
                    new YamlScalarNode("logo-matching-tolerance"),
                    new YamlScalarNode("0.90"),
                    new YamlScalarNode("password-matching-tolerance"),
                    new YamlScalarNode("0.80"),
                    new YamlScalarNode("debug"),
                    new YamlScalarNode("false")
                ),
                new YamlScalarNode("client-load-timeout"),
                new YamlScalarNode("30")
            );

            // create settings variable
            YamlMappingNode settings;

            // check if settings exist
            if (File.Exists(settingsFile))
            {
                try
                {
                    // load settings from yaml
                    var loadedSettings = ReadYaml<YamlMappingNode>(settingsFile);

                    // merge settings if not empty, use default if it is
                    if (loadedSettings != null)
                    {
                        settings = MergeMappingNodes(defaultSettings, loadedSettings, false);
                    }
                    else
                    {
                        Logger.Info("Settings file is empty, using default settings.");
                        
                        settings = defaultSettings;
                    }
                }
                catch (Exception ex)
                {
                    Logger.PrintException(ex);
                    Logger.Warn("Failed to parse YAML, reverting to default settings.");
                    
                    settings = defaultSettings;
                }
            }
            else
            {
                Logger.Info("Settings file does not exist, using default settings.");
                
                settings = defaultSettings;
            }

            // wrap in try/catch in case there's a parsing error
            try
            {
                // set vars to loaded values
                LogoMatchTolerance = float.Parse(((YamlScalarNode)settings["login-detection"]["logo-matching-tolerance"]).Value, CultureInfo.InvariantCulture);
                PasswordMatchTolerance = float.Parse(((YamlScalarNode)settings["login-detection"]["password-matching-tolerance"]).Value, CultureInfo.InvariantCulture);
                ClientDetectionDebug = bool.Parse(((YamlScalarNode)settings["login-detection"]["debug"]).Value);
                ClientTimeout = int.Parse(((YamlScalarNode)settings["client-load-timeout"]).Value) * 1000;
            }
            catch (Exception ex)
            {
                Logger.PrintException(ex);
                Logger.Warn("Failed to parse YAML values, reverting to default settings.");

                // use default settings
                settings = defaultSettings;
            }

            // write yaml
            WriteYaml(settingsFile, settings);
        }

        private static T ReadYaml<T>(string file) where T : YamlNode
        {
            T read = default(T);

            using (var reader = new StreamReader(file))
            {
                var deserializer = new Deserializer();
                var parser = new Parser(reader);
                read = deserializer.Deserialize<T>(parser);
            }

            return read;
        }

        private static void WriteYaml<T>(string file, T yaml) where T : YamlNode
        {
            using (var writer = new StreamWriter(file))
            {
                var serializer = new SerializerBuilder().EnsureRoundtrip().Build();
                writer.Write(serializer.Serialize(yaml));
            }
        }

        private static YamlMappingNode MergeMappingNodes(YamlMappingNode a, YamlMappingNode b, bool mergeNonSharedValues = true)
        {
            // create merged values node
            YamlMappingNode merged = new YamlMappingNode();

            // iterate through a's items
            foreach (var item in a.Children)
            {
                // check if b contains this item
                if (b.Children.ContainsKey(item.Key))
                {
                    // if both values are mapping nodes, add merged mapped nodes; if not, add b's value
                    if (item.Value is YamlMappingNode && b[item.Key] is YamlMappingNode)
                        merged.Children.Add(item.Key, MergeMappingNodes((YamlMappingNode)item.Value, (YamlMappingNode)b[item.Key], mergeNonSharedValues));
                    else
                        merged.Children.Add(item.Key, b[item.Key]);
                }
                else
                {
                    // add item to merged
                    merged.Children.Add(item);
                }
            }

            // if we want to merged non-shared values, add all of b's children that aren't already in merged
            if (mergeNonSharedValues)
                foreach (var item in b.Children)
                    if (!merged.Children.ContainsKey(item.Key))
                        merged.Children.Add(item);

            // log loaded values to trace
            foreach (var item in merged)
                if (item.Key is YamlScalarNode && item.Value is YamlScalarNode)
                    Logger.Trace("{0} = {1}", ((YamlScalarNode)item.Key).Value, ((YamlScalarNode)item.Value).Value);

            // return merged values
            return merged;
        }
    }
}
