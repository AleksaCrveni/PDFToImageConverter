typedef unsigned char uint8;
typedef unsigned short uint16;
typedef unsigned int uint32;

struct Buf {
    unsigned char *data{};
    int pos{};
    int size{};
};

uint8 buf_get8(Buf *b) {
    if (b->pos >= b->size) {
        return 0;
    }
    return b->data[b->pos++];
}

uint32 buf_get(Buf *b, int n) {
    uint32 v = 0;
    //assert(n >= 1 && n <= 4);
    for (int i = 0; i < n; i++) {
        v = (v << 8) | buf_get8(b);
    }
    return v;
}

int main(int argc, char const *argv[])
{
  /* code */
  return 0;
}
