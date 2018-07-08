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

using System.IO;
using YamlDotNet.RepresentationModel;

namespace LoLAutoLogin
{
    public class ClientSettings
    {
        internal string Locale { get; }
        internal string Region { get; }
        internal bool RemembersUsername { get; }

        private ClientSettings(string locale, string region, bool remembersUsername)
        {
            Locale = locale;
            Region = region;
            RemembersUsername = remembersUsername;
        }

        internal static ClientSettings FromFile(string filePath)
        {
            Logger.Info($"Loading client info from \"{filePath}\"");

            if (!File.Exists(filePath))
                throw new FileNotFoundException();

            YamlMappingNode root = Util.ReadYaml<YamlMappingNode>(filePath);

            string locale = (root?["install"]?["globals"]?["locale"] as YamlScalarNode)?.Value;
            string region = (root?["install"]?["globals"]?["region"] as YamlScalarNode)?.Value;
            bool remembersUsername = bool.Parse((root?["install"]?["login-remember-me"]?["rememberMe"] as YamlScalarNode)?.Value);

            return new ClientSettings(locale, region, remembersUsername);
        }
    }
}
