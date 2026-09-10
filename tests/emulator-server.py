"""Controlled test host; receives JSON commands from a local test file."""
import subprocess, pathlib, time, json
root=pathlib.Path(__file__).resolve().parents[1]
p=subprocess.Popen([str(root/'build/InteractiveHarness.exe'),'0123456789abcdef'],stdin=subprocess.PIPE,stdout=open(root/'build/emulator-host.log','w'),text=True,encoding='utf-8')
command=root/'build/emulator-command.jsonl'
command.write_text('')
try:
    count=0
    while True:
        lines=command.read_text(encoding='utf-8').splitlines()
        for line in lines[count:]:
            json.loads(line)
            p.stdin.write(line+'\n');p.stdin.flush()
        count=len(lines)
        time.sleep(.1)
finally:p.terminate()
