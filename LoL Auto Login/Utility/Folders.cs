﻿// Copyright © 2015-2019 Nicolas Gnyra

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

namespace LoLAutoLogin.Utility
{
    internal static class Folders
    {
        public static readonly string Configuration = Path.Combine(Directory.GetCurrentDirectory(), "Config");
        public static readonly string Logs = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "LoL Auto Login Logs");
        public static readonly string Debug = Path.Combine(Directory.GetCurrentDirectory(), "Debug");
    }
}
