"""Black-box tests against the actual Windows TLS server implementation."""
import hashlib, hmac, json, secrets, socket, ssl, subprocess, time
from pathlib import Path
root=Path(__file__).resolve().parents[1]
key=secrets.token_hex(8)
proc=subprocess.Popen([str(root/'build/NetworkHarness.exe'),key],stdout=subprocess.PIPE,text=True)
assert proc.stdout.readline().strip()=='READY'
passed=[]
def check(ok,name):
    assert ok,name
    passed.append(name)
def open_peer():
    ctx=ssl.SSLContext(ssl.PROTOCOL_TLS_CLIENT);ctx.check_hostname=False;ctx.verify_mode=ssl.CERT_NONE
    s=ctx.wrap_socket(socket.create_connection(('127.0.0.1',37128)),server_hostname='Velixa');s.settimeout(10)
    r=s.makefile('rwb',buffering=0);hello=json.loads(r.readline())
    basis=hello['nonce']+'|'+hashlib.sha256(s.getpeercert(binary_form=True)).hexdigest()
    expected=hmac.new(key.encode(),(basis+'|server').encode(),hashlib.sha256).hexdigest()
    check(hmac.compare_digest(hello['proof'],expected),'server certificate-bound proof')
    return s,r,basis
def auth(r,basis,proof=None):
    proof=proof or hmac.new(key.encode(),(basis+'|client').encode(),hashlib.sha256).hexdigest()
    r.write((json.dumps(dict(t='auth',proof=proof,name='Test Android',kind='Android',id='test',w=1080,h=2400))+'\n').encode())
try:
    s,r,basis=open_peer();auth(r,basis)
    check(json.loads(r.readline())['t']=='ready','authenticated connection accepted')
    enter=json.loads(r.readline());check(enter['edge']=='left' and enter['x']==.25,'edge and normalized coordinates delivered')
    event=json.loads(r.readline());check(event['text']=='नमस्ते' and event['down'],'Unicode key payload preserved')
    check(json.loads(r.readline())['t']=='ping','heartbeat delivered');r.write(b'{"t":"pong"}\n')
    r.close();s.close()
    s,r,basis=open_peer();auth(r,basis,'0'*64);check(r.readline()==b'','incorrect pairing proof rejected');r.close();s.close()
    s,r,basis=open_peer();r.write(b'x'*17000+b'\n')
    try: data=r.readline();check(data==b'','oversize authentication rejected')
    except (ConnectionResetError,ssl.SSLError):check(True,'oversize authentication rejected')
    r.close();s.close()
    s,r,basis=open_peer();auth(r,basis)
    started=time.monotonic()
    while r.readline():pass
    check(time.monotonic()-started<8,'unresponsive peer disconnected by watchdog');r.close();s.close()
    u=socket.socket(socket.AF_INET,socket.SOCK_DGRAM);u.settimeout(2);u.sendto(b'VELIXA_DISCOVER_1',('127.0.0.1',37129))
    check(u.recv(512).startswith(b'VELIXA_1|'),'local discovery reply');u.close()
finally:
    proc.terminate();proc.wait()
(root/'build/network-tests.txt').write_text('\n'.join(passed)+'\n',encoding='utf-8')
print(f'{len(passed)} network checks passed')
