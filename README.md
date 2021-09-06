# This program is no longer maintained and will not work properly.

# LoL Auto Login

[![Jenkins](https://img.shields.io/jenkins/s/https/ci.gnyra.com/job/LoL-Auto-Login/job/develop.svg?style=flat-square)](https://ci.gnyra.com/blue/organizations/jenkins/LoL-Auto-Login) ![license](https://img.shields.io/github/license/nicoco007/lol-auto-login.svg?style=flat-square) [![GitHub release](https://img.shields.io/github/release/nicoco007/LoL-Auto-Login.svg?style=flat-square)](https://github.com/nicoco007/LoL-Auto-Login/releases/latest)

Automatic login for League of Legends. See the [official website page](https://www.nicoco007.com/lol-auto-login/) for more information.

Running LoL Auto Login requires the .NET Framework 4.5 Runtime. It is installed by default as of Windows 8. If you do not have it, you can download it [from Microsoft's website](https://www.microsoft.com/net/download/dotnet-framework-runtime/net452).

# Configuration
You can configure LoL Auto Login through the `LoLAutoLoginSettings.yaml` file in the Config directory.

* **login-detection**:
  * **debug** (*default: false*) Enabling this option saves debug images to the `Debug` folder inside your home League of Legends folder for debugging purposes. You may be asked to enable this if you submit an issue.
  * **low-threshold** (*default: 0*) Low threshold for the Canny edge detector.
  * **high-threshold** (*default: 20*) High threshold for the Canny edge detector.
  * **delay** (*default: 500*) Number of milliseconds to wait before entering password. Can be useful if buttons aren't working in the League client (see [this issue](https://github.com/nicoco007/LoL-Auto-Login/issues/24)).
* **client-load-timeout** (*default: 30*) How long to wait (in seconds) before giving up on searching for the client.
* **log-level** (*default: "info"*) How much stuff to log. Valid options are, in decreasing order of spamminess: `trace`, `debug`, `info`, `warn`, `error`, `fatal`.
* **log-to-file** (*default: true*) Whether to log to file or not.
* **check-for-updates** (*default: true*) Whether to automatically check for updates on startup or not.

# Acknowledgements
LoL Auto Login uses the following 3<sup>rd</sup> party libraries:
* [Accord.NET Framework](http://accord-framework.net/) &ndash; [GNU LGPL License](https://github.com/accord-net/framework/blob/development/LICENSE)
* [Json.NET](https://github.com/JamesNK/Newtonsoft.Json) &ndash; [MIT License](https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md)
* [Fody](https://github.com/Fody/Fody) &ndash; [MIT License](https://github.com/Fody/Fody/blob/master/License.txt)
* [YamlDotNet](https://github.com/aaubry/YamlDotNet) &ndash; [MIT License](https://github.com/aaubry/YamlDotNet/blob/master/LICENSE)
