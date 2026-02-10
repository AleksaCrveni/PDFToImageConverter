#Getting the eexec binary, make sure you exclude the ascii part in the end, after the binary portion 
text = open('Type1FontFileExample.txt', errors="ignore").read()
raw_hex = text.split('eexec')[1]
decarr = list()
count = 0
hex_code = str()

#Converting pairs of the hexadecimal digits to decimal, e.g. ff -> 255, and storing it in an array decarr
for i in range(len(raw_hex)):
    if raw_hex[i] == '\n':
        decarr.append(raw_hex[i])
        continue
    else:
        hex_code = hex_code + raw_hex[i]
        count += 1
        if count == 2:
            decarr.append(int(hex_code, 16))
            count = 0
            hex_code = str()

c1 = 52845
c2 =  22719
R = 55665
p = list()
for i in range(0,len(decarr)):
    if decarr[i] is not '\n':
        p.append(decarr[i]^(R >> 8))
        R = ((decarr[i] + R)*c1 + c2) & ((1 << 16) - 1)
    else:
        p.append(decarr[i])
decrypted = list()
for i in range(len(p)):
    if p[i] is not '\n':
        decrypted.append(chr(p[i]))
    else:
        decrypted.append(p[i])

print(decrypted)