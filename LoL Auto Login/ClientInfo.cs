using System.IO;
using YamlDotNet.RepresentationModel;

namespace LoLAutoLogin
{
    public class ClientInfo
    {
        internal string Locale { get; }
        internal string Region { get; }
        internal bool RemembersUsername { get; }

        internal ClientInfo(string yamlPath)
        {
            Logger.Info($"Loading client info from \"{yamlPath}\"");

            if (!File.Exists(yamlPath))
                throw new FileNotFoundException();

            var root = Util.ReadYaml<YamlMappingNode>(yamlPath);

            Locale = (root?["install"]?["globals"]?["locale"] as YamlScalarNode)?.Value;
            Region = (root?["install"]?["globals"]?["region"] as YamlScalarNode)?.Value;
            RemembersUsername = bool.Parse((root?["install"]?["login-remember-me"]?["rememberMe"] as YamlScalarNode)?.Value);
        }
    }
}
