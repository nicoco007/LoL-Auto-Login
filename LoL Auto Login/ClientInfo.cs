using System.IO;
using YamlDotNet.RepresentationModel;

namespace LoLAutoLogin
{
    public class ClientInfo
    {
        internal string Locale { get; }
        internal string Region { get; }
        internal bool RemembersUsername { get; }

        private ClientInfo(string locale, string region, bool remembersUsername)
        {
            Locale = locale;
            Region = region;
            RemembersUsername = remembersUsername;
        }

        internal static ClientInfo FromFile(string filePath)
        {
            Logger.Info($"Loading client info from \"{filePath}\"");

            if (!File.Exists(filePath))
                throw new FileNotFoundException();

            YamlMappingNode root = Util.ReadYaml<YamlMappingNode>(filePath);

            string locale = (root?["install"]?["globals"]?["locale"] as YamlScalarNode)?.Value;
            string region = (root?["install"]?["globals"]?["region"] as YamlScalarNode)?.Value;
            bool remembersUsername = bool.Parse((root?["install"]?["login-remember-me"]?["rememberMe"] as YamlScalarNode)?.Value);

            return new ClientInfo(locale, region, remembersUsername);
        }
    }
}
