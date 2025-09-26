import re
import zlib

pdf = open("Report.pdf", "rb").read()
stream = re.compile(rb'.*?FlateDecode.*?stream(.*?)endstream', re.S)
i = 0
for s in stream.findall(pdf):
    s = s.strip(b'\r\n')
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