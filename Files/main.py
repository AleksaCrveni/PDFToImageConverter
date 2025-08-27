import re
import zlib

pdf = open("file-sample_150kB.pdf", "rb").read()
stream = re.compile(rb'.*?FlateDecode.*?stream(.*?)endstream', re.S)
i = 0
for s in stream.findall(pdf):
    s = s.strip(b'\r\n')
    if i != 0:
      continue
    i = 1
    ss = []
    try:
        #for c in s:
          #ss.append(int(c))
        #print(ss)
        #print(len(ss))
        #print(s)
        print(zlib.decompress(s))
        print("")
    except:
        pass