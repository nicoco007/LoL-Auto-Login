import os
import re

build = os.environ["BUILD_NUMBER"] if "BUILD_NUMBER" in os.environ else "0"

expr = re.compile(r"\[assembly: Assembly(\w*)Version\(\"(\d+)\.(\d+)\.(\d+)\.(\d+)\"\)\]", re.IGNORECASE)

with open("LoL Auto Login\Properties\AssemblyInfo.cs", "r+", encoding="utf-8") as f:
    read_lines = f.readlines()
    write_lines = []

    for i, line in enumerate(read_lines):
        if (expr.search(line)):
            new_line = expr.sub(r'[assembly: Assembly\1Version("\2.\3.\4.{}")]'.format(build), line)
            print("{} -> {} on line {}".format(line.strip(), new_line.strip(), i + 1))
            write_lines.append(new_line)
        else:
            write_lines.append(line)

    f.seek(0)
    f.truncate(0)

    for line in write_lines:
        f.write(line)
