#include <stdio.h>
void Test(int x)
{
  printf("%d", x);
}

int main(int argc, char const *argv[])
{
  float f = 8.56f;
  Test((int)f);
}

