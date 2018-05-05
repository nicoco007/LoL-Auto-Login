# LoL Auto Login

[![Jenkins](https://img.shields.io/jenkins/s/https/ci.gnyra.com/job/LoL-Auto-Login/job/master.svg?style=flat-square)](https://ci.gnyra.com/blue/organizations/jenkins/LoL-Auto-Login) [![GitHub release](https://img.shields.io/github/release/nicoco007/LoL-Auto-Login.svg?style=flat-square)](https://github.com/nicoco007/LoL-Auto-Login/releases/latest)

Automatic login for League of Legends. See the [official website page](https://www.nicoco007.com/other-stuff/lol-auto-login/) for more information.

Running LoL Auto Login requires the .NET Framework 4.7.1 Runtime. It is already included in the Windows 10 Fall Creators Update. If you do not have it, you can download it [from Microsoft's website](https://www.microsoft.com/net/download/dotnet-framework-runtime).

# Known issues
* The client window is sometimes not detected correctly and the program fails to log in. The client tends to spawn multiple windows, and the program sometimes gets confused as to which window is the right one.

# Configuration
You can configure LoL Auto Login through the `LoLAutoLoginSettings.yaml` file in the Config directory.

* **login-detection**:
  * **tolerance** (*default: 0.90*) The minimum correlation value when searching for the password box. If LoL Auto Login doesn't seem to detect your client, you should try reducing this value. It shouldn't have to be less than 0.50.
  * **enable-click** (*default: true*) Whether LoL Auto Login should click on the password box once it finds it or not. This forces the password box to be focused, but if you don't like the cursor moving you can disable it by setting this to `false`.
  * **debug** (*default: false*) Enabling this option saves debug images to the `Debug` folder inside your home League of Legends folder for debugging purposes. You may be asked to enable this if you submit an issue.
* **client-load-timeout** (*default: 30*) How long to wait (in seconds) before giving up on searching for the client.
* **log-level** (*default: "info"*) How much stuff to log. Valid options are, in order of spam, `trace`, `debug`, `info`, `warn`, `error`, `fatal`.

# Acknowledgements
LoL Auto Login uses the following 3<sup>rd</sup> party libraries:
* [AutoIt](https://www.autoitscript.com) by Jonathan Bennett and the AutoIt Team &ndash; [AutoIt License](https://www.autoitscript.com/autoit3/docs/license.htm)
* [Fody](https://github.com/Fody/Fody) by Simon Cropp and Cameron MacFarland &ndash; [MIT License](https://github.com/Fody/Fody/blob/master/License.txt)
* [YamlDotNet](https://github.com/aaubry/YamlDotNet) by Antoine Aubry &ndash; [MIT License](https://github.com/aaubry/YamlDotNet/blob/master/LICENSE)
