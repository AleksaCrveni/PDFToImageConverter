// most of this code was take from example at https://github.com/justinmeiners/stb-truetype-example/blob/master/main.c
#include <stdint.h>

#include <stdio.h>
#include <stdlib.h>


#define STB_IMAGE_WRITE_IMPLEMENTATION
#include "stb_image_write.h"

#define STB_TRUETYPE_IMPLEMENTATION
#include "stb_truetype.h"



int main(int argc, char const *argv[])
{
  long size;
  unsigned char* fontBuffer;
  FILE* fontFile = fopen("W:/PDFToImageConverter/Files/TT1FontInfo.txt", "rb");
  fseek(fontFile, 0, SEEK_END);
  size = ftell(fontFile);
  fseek(fontFile, 0, SEEK_SET);
  int fontSize = 36;
  fontBuffer = malloc(size);
  
  // read into buffer and dodnt keep file open
  fread(fontBuffer, size, 1, fontFile);
  fclose(fontFile);


  stbtt_fontinfo info;
  if (!stbtt_InitFont(&info, fontBuffer, 0))
  {
    printf("Failed to init font!");
    exit(-1);
  }

  // this should be page width
  int bitmapWidth = 612;
  // this should be Ascent - Descent or something similar ?? basically max line height
  // but it font work for all fonts, so it may be something big that can just be reused
  // and read with boundaries
  int bitmapHeight = 792;
  
  // this is Ascent - Descent
  int lineHeight = 16;

  unsigned char* bitmap = calloc(bitmapWidth * bitmapHeight, sizeof(unsigned char));

  // I think we should use this for scale?? or just value from Text line matrix or something similar
  float scaleFactor = stbtt_ScaleForPixelHeight(&info, lineHeight);

  char* textToTranslate = "Nova Dusk PDF";
  int indexes[] = {4,8,11, 6, 1, 2, 10, 9, 7, 1, 5, 2, 3};
  char* textToTranslate2nd = "Second Row";

  int x = 0;
  // ascent and descent are defined in font descriptor, use those I think over getting i from  the font
  int ascent, descent, lineGap;
  stbtt_GetFontVMetrics(&info, &ascent, &descent, &lineGap);
  
  /*int x0, y0, x1, y1;
  stbtt_GetFontBoundingBox(&info, &x0, &y0, &x1, &y1);*/

  ascent = roundf(ascent * scaleFactor);
  descent = roundf(descent * scaleFactor);


  int i =0;
  int len = strlen(textToTranslate);
  int baseline = 0;
  int useIndex = 1;
  for (i = 0; i < len; ++i)
  {
    /*if (textToTranslate[i] == '@')
    {
      baseline += lineHeight;
      x = 0;
    }*/
      
    int ax; // charatcter width
    int lsb; // left side bearing
    if (useIndex)
      stbtt_GetGlyphHMetrics(&info, indexes[i], &ax, &lsb);
    else
      stbtt_GetCodepointHMetrics(&info, textToTranslate[i], &ax, &lsb);
    //stbtt_GetGlyphHMetrics(&info, )

    int c_x0, c_y0, c_x1, c_y1;
    if (useIndex)
      stbtt_GetGlyphBitmapBox(&info, indexes[i], scaleFactor, scaleFactor, &c_x0, &c_y0, &c_x1, &c_y1);
    else
      stbtt_GetCodepointBitmapBox(&info, textToTranslate[i], scaleFactor, scaleFactor, &c_x0, &c_y0, &c_x1, &c_y1);
    
    // char height
    int y = ascent + c_y0 + baseline;

    int charOffset = x + roundf(lsb * scaleFactor) + (y * bitmapWidth);
    if (useIndex)
      stbtt_MakeGlyphBitmap(&info, bitmap + charOffset, c_x1 - c_x0, c_y1 - c_y0, bitmapWidth, scaleFactor, scaleFactor, indexes[i]);
    else
      stbtt_MakeCodepointBitmap(&info, bitmap + charOffset, c_x1 - c_x0, c_y1 - c_y0, bitmapWidth, scaleFactor, scaleFactor, textToTranslate[i]);
    // advance x
    x += roundf(ax * scaleFactor);

    // kerning

   /* int kern;
    kern = stbtt_GetCodepointKernAdvance(&info, textToTranslate[i], textToTranslate[i + 1]);
    x += roundf(kern * scaleFactor);*/
  }
  FILE *fptr = fopen("stdByteOutput_letter_T.txt", "w");
  for (int i = 0; i < bitmapHeight * bitmapWidth; i++){
    if (bitmap[i] > 0)
      fprintf(fptr, "%d \n", i);
  }

  fclose(fptr);
  
  stbi_write_png("outMine.png", bitmapWidth, bitmapHeight, 1, bitmap, bitmapWidth);
  free(fontBuffer);
  free(bitmap);

  printf("test");
  return 0;
}