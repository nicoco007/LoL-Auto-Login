using System;

namespace LoLAutoLogin
{

    [Flags]
    internal enum MouseEventFlags : uint
    {

        LEFTDOWN = 0x00000002,
        LEFTUP = 0x00000004,
        MIDDLEDOWN = 0x00000020,
        MIDDLEUP = 0x00000040,
        MOVE = 0x00000001,
        ABSOLUTE = 0x00008000,
        RIGHTDOWN = 0x00000008,
        RIGHTUP = 0x00000010,
        WHEEL = 0x00000800

    }

}
