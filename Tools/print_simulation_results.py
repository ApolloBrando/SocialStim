"""Print captured balance receipts from NUnit TRX, without requiring another simulation."""
from pathlib import Path
import xml.etree.ElementTree as ET

for path in sorted(Path("TestResults").glob("*.trx")):
    root = ET.parse(path).getroot()
    for node in root.iter():
        if node.tag.rsplit("}", 1)[-1] == "StdOut" and node.text:
            for line in node.text.splitlines():
                if line.startswith(("SIM_BASELINE ", "SIM_FAILURE ")):
                    print(line)
