import os
import re

build = os.environ["BUILD_NUMBER"] if "BUILD_NUMBER" in os.environ else 0

expr = re.compile(r"\[assembly: Assembly(\w*)Version\(\"(\d+)\.(\d+)\.(\d+)\.(\d+)\"\)\]", re.IGNORECASE)

with open("LoL Auto Login\Properties\AssemblyInfo.cs", "r+", encoding="utf-8") as f:
    read_lines = f.readlines()
    write_lines = []

    for line in read_lines:
        write_lines.append(expr.sub(r'[assembly: Assembly\1Version("\2.\3.\4.{}")]'.format(build), line))

    f.seek(0)
    f.truncate(0)

    for line in write_lines:
        f.write(line)
