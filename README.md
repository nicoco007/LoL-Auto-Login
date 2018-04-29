# LoL Auto Login
[![Jenkins](https://img.shields.io/jenkins/s/https/ci.gnyra.com/job/LoL-Auto-Login/job/master.svg?style=flat-square)](https://ci.gnyra.com/blue/organizations/jenkins/LoL-Auto-Login)

Automatic login for League of Legends. See the [official website page](https://www.nicoco007.com/other-stuff/lol-auto-login/) for more information.

Running LoL Auto Login requires the .NET Framework 4.7.1 Runtime. It is already included in the Windows 10 Fall Creators Update. If you do not have it, you can download it [from Microsoft's website](https://www.microsoft.com/net/download/dotnet-framework-runtime).

# Known issues
* The client window is sometimes not detected correctly and the program fails to log in. The client tends to spawn multiple windows, and the program sometimes gets confused as to which window is the right one.

# Acknowledgements
LoL Auto Login uses the following 3<sup>rd</sup> party libraries:
* [AutoIt](https://www.autoitscript.com) by Jonathan Bennett and the AutoIt Team &ndash; [AutoIt License](https://www.autoitscript.com/autoit3/docs/license.htm)
* [Fody](https://github.com/Fody/Fody) by Simon Cropp and Cameron MacFarland &ndash; [MIT License](https://github.com/Fody/Fody/blob/master/License.txt)
* [YamlDotNet](https://github.com/aaubry/YamlDotNet) by Antoine Aubry &ndash; [MIT License](https://github.com/aaubry/YamlDotNet/blob/master/LICENSE)
