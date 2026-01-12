#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <tchar.h>
#include <Windows.h>
#include <string.h>
#include <fstream>
#include <iostream>
#include <thread>
#include <vector>
#define STB_TRUETYPE_IMPLEMENTATION
#include "rasterizerVisualizer/stb_truetype.h"

using namespace std;

typedef int8_t int8;
typedef int16_t int16;
typedef int32_t int32;
typedef int64_t int64;
typedef int32 bool32;

typedef uint8_t uint8;
typedef uint16_t uint16;
typedef uint32_t uint32;
typedef uint64_t uint64;

typedef float real32;
typedef double real64;

#define global_var static

global_var int RENDER_WIDTH = 15;
global_var int RENDER_HEIGHT = 15;
global_var int WINDOW_WIDTH = 500;
global_var int WINDOW_HEIGHT = 500;

const TCHAR* dir = _T("..\\..\\RasterPlayground\\bin\\Debug\\net8.0\\Single\\");
const char* filePath = "..\\..\\RasterPlayground\\bin\\Debug\\net8.0\\Single\\data.txt";

struct win32_offscreen_buffer
{
	BITMAPINFO Info;
	void *Memory;
};
struct win32_window_dimension
{
	int Height;
	int Width;
};

int LoadData();
void Observe();
void Win32CopyBufferToWindow(
	HDC DeviceContext, int WindowWidth,  int WindowHeight,
	win32_offscreen_buffer *Buffer);
void RenderWeirdGradient(win32_offscreen_buffer *Buffer, int BlueOffset, int GreenOffset);
win32_window_dimension Win32GetWindowDimension(HWND Window);
void HotLoadBuffer(win32_offscreen_buffer *Buffer);
void RenderText();
void ConvertBitmapToWindowsBitmap(vector<int>* orderToDraw, vector<int>* xPositions, unsigned char *bitmap);

static win32_offscreen_buffer GlobalBuffer;
static bool ShouldRender = true;
bool GlobalRunning = false;

LRESULT CALLBACK Win32MainWindowCallback(
  HWND Window,
  UINT Message,
  WPARAM WParam,
  LPARAM LParam)
{
	LRESULT Result = 0;
	switch (Message)
	{
		case WM_DESTROY:
		{
			GlobalRunning = false;
		} break;
		case WM_CLOSE:
		{
			// PostQuitMessage(0); can do this to post quit message to our queue, but can also do static var;
			GlobalRunning = false;
		} break;
		case WM_ACTIVATEAPP:
		{
			OutputDebugStringA("WM_ACTIVATEAPP\n");
		} break;
		case WM_PAINT:
		{
			PAINTSTRUCT Paint;
			HDC DeviceContext = BeginPaint(Window, &Paint);
			
			int X = Paint.rcPaint.left;
			int Y = Paint.rcPaint.top;
			int Height = Paint.rcPaint.bottom  - Paint.rcPaint.top;
			int Width = Paint.rcPaint.right - Paint.rcPaint.left;
			win32_window_dimension Dimension = Win32GetWindowDimension(Window);
			Win32CopyBufferToWindow(
				DeviceContext, Dimension.Width, Dimension.Width,
				&GlobalBuffer);
			EndPaint(Window, &Paint);
			
		} break;
		default:
		{
			//OutputDebugSTringA("default\n");
			// Default win proc that can handle all codes with default behaviour
			Result = DefWindowProc(Window, Message, WParam, LParam);
		} break;
	}

	return Result;
}



int WINAPI WinMain(HINSTANCE Instance, HINSTANCE PrevInstance, PSTR CommandLine, int ShowCode)
{  
  thread t(RenderText);

	LARGE_INTEGER PerfCounterFreqResult;
	QueryPerformanceFrequency(&PerfCounterFreqResult);
	int64 PerfCounterFreq = PerfCounterFreqResult.QuadPart;

  GlobalBuffer = {};
  // 4 is for bytes per pixel
  GlobalBuffer.Memory = VirtualAlloc(0, RENDER_WIDTH*RENDER_HEIGHT*4, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
  GlobalBuffer.Info = {};
  GlobalBuffer.Info.bmiHeader.biSize = sizeof(GlobalBuffer.Info.bmiHeader);
	GlobalBuffer.Info.bmiHeader.biWidth = RENDER_WIDTH;
	// negative so bitmap is top to btottom and origin is upper left corner
	GlobalBuffer.Info.bmiHeader.biHeight = -RENDER_HEIGHT;
	GlobalBuffer.Info.bmiHeader.biPlanes = 1;
	// 8 bits each for Red, Green, Blue and 8 extra padded for alignment on 4B boundaries 
	GlobalBuffer.Info.bmiHeader.biBitCount = 32;
	GlobalBuffer.Info.bmiHeader.biCompression = BI_RGB;

  // Init struct with 0 values
	WNDCLASS WindowClass = {};

	// this will pain entire window when streching window horizontally or vertically
	WindowClass.style = CS_HREDRAW|CS_VREDRAW|CS_OWNDC;
	//Pointer to the function (pretty much registering callback)
	WindowClass.lpfnWndProc = Win32MainWindowCallback;
	WindowClass.hInstance = Instance;
	// WindowClass.hIcon
	WindowClass.lpszClassName = "Program";
	win32_offscreen_buffer b = {};
	b.Memory = VirtualAlloc(0, RENDER_WIDTH * RENDER_HEIGHT * 4, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
	HotLoadBuffer(&GlobalBuffer);
	//RenderWeirdGradient(&GlobalBuffer, 32, 42);
	LARGE_INTEGER LastCounter;
	QueryPerformanceCounter(&LastCounter);
	uint64 LastCycleCount = __rdtsc();
  if (RegisterClass(&WindowClass))
  {
    HWND Window = 
      CreateWindowEx(
        0,
        WindowClass.lpszClassName,
        "Program",
        WS_SYSMENU | WS_CAPTION | WS_VISIBLE,
				//WS_OVERLAPPEDWINDOW | WS_VISIBLE,
        CW_USEDEFAULT,
        CW_USEDEFAULT,
        WINDOW_WIDTH,
        WINDOW_HEIGHT,
        0,
        0,
        Instance,
        0);

    if (Window)
    {
      // Can do this sicnce we add CS_OWNDC
			HDC DeviceContext = GetDC(Window);
			// Have to start pulling messages from queue or kernel wont sent it
			MSG Message;
			GlobalRunning = true;
      while (GlobalRunning)
      {
        MSG Message;
        while(PeekMessage(&Message, 0, 0, 0, PM_REMOVE))
        {
          if (Message.message == WM_QUIT)
          {
            GlobalRunning = false;
          }
          TranslateMessage(&Message);
          DispatchMessage (&Message);
        }

				win32_window_dimension Dimension = Win32GetWindowDimension(Window);
				if (ShouldRender)
				{
					// render just once in a loop since i just want to display static image and not burn my GPU
					ShouldRender = false; 
						Win32CopyBufferToWindow(
							DeviceContext, Dimension.Width, Dimension.Height,
							&GlobalBuffer);	
				}


				LARGE_INTEGER EndCounter;
				uint64 EndCycleCount = __rdtsc();
				QueryPerformanceCounter(&EndCounter);

			 	int64 CounterElapsed = EndCounter.QuadPart - LastCounter.QuadPart;
				real32 MSPerFrame = (real32)(((1000.0f*(real32)CounterElapsed) / (real32)PerfCounterFreq));
				//int32 d = (CounterElapsed * (1/PerfCounterFreq));
				real32 FPS = (real32)PerfCounterFreq / (real32)CounterElapsed;
				uint64 CyclesElapsed = EndCycleCount - LastCycleCount;
				// Mega cycles per second - so whatever value here we executed MCPF * 1000 * 1000 instructions
				// this is just for easier viewing 
				real32 MCPF = (real32)CyclesElapsed / (1000.0f * 1000.0f);
				char Buffer[256];
				
				//sprintf(Buffer, "Milliseconds/frame: %fms FPS: %f Cycles: %f \n", MSPerFrame, FPS, MCPF);
				//OutputDebugString(Buffer);
				LastCounter = EndCounter;
				LastCycleCount = EndCycleCount;
      }
    }
  }
  return EXIT_SUCCESS;
}

void Observe()
{
 DWORD dwNotificationFlags =
    FILE_NOTIFY_CHANGE_LAST_WRITE
    | FILE_NOTIFY_CHANGE_CREATION
    | FILE_NOTIFY_CHANGE_FILE_NAME;
  HANDLE hDir = CreateFile(
    dir,
    FILE_LIST_DIRECTORY,
    FILE_SHARE_WRITE | FILE_SHARE_READ | FILE_SHARE_DELETE,
    NULL,
    OPEN_EXISTING,
    FILE_FLAG_BACKUP_SEMANTICS,
    NULL
  );
  int nCounter = 0;

  FILE_NOTIFY_INFORMATION strFileNotifyInfo[16384];
  DWORD dwBytesReturned = 0; 
  while(true)
  {
 
    if(ReadDirectoryChangesW( hDir, (LPVOID)&strFileNotifyInfo, sizeof(strFileNotifyInfo), FALSE, FILE_NOTIFY_CHANGE_LAST_WRITE, &dwBytesReturned, NULL, NULL) == 0)
    {
      // idk what this is
      printf("IF true\n");
    }
    else
    {
      // no need to worry about duplicates because it will always be one file in directory we are looking at
			HotLoadBuffer(&GlobalBuffer);
    }  

  }
}

void Win32CopyBufferToWindow(
	HDC DeviceContext, int WindowWidth,  int WindowHeight,
	win32_offscreen_buffer *Buffer)
{
	// TODO: Fix aspect ratio
	// Pretty much copy rectangle from our buffer to the screen
	// That is hwy source and dest coords are the same
	int result = StretchDIBits(
		DeviceContext,
		/*
		X,Y,Width,Height,
		X,Y,Width,Height,
		*/
		0, 0, WindowWidth, WindowHeight,
		0, 0, RENDER_WIDTH, RENDER_HEIGHT,
		Buffer->Memory,
		&Buffer->Info,
		DIB_RGB_COLORS,
		SRCCOPY);
	if (result == 0)
		printf("Error in STRECHDIBBITS \n", result);
}

void RenderWeirdGradient(win32_offscreen_buffer *Buffer, int BlueOffset, int GreenOffset)
{
	// TODO lets see what o ptimized does
	// byte array pretty much
	uint8 *Row = (uint8 *)Buffer->Memory;
	for (int Y = 0; Y < RENDER_HEIGHT; ++Y)
	{
		// uint8 *Pixel  = (uint8 *)Row;
		uint32 *Pixel = (uint32 *)Row;
		for (int X = 0; X < RENDER_WIDTH; ++X)
		{
			/* 8 - bit red 8 bits of green 8 bits of blue and 8 bits of padding
			Pixel in memory: RR GG BB xx
			But due to little endian format of our CPU bytes are loaded in a way where
			least significant byte is stored at  lowest memory address and so on, so it appears that bytes are read from right to left
			so RR GG BB xx will be read as 0xXXBBGGRR (backwards)
			
			BUT windows people didn't like that so they reorder it in memory where blue bytes are first  so in memory its BB GG RR xx
			0xXXRRGGBB
			
			// Blue bits
			*Pixel = (uint8)(X + BlueOffset);
			++Pixel;

			// Green bits
			*Pixel = (uint8)(Y + GreenOffset);
			++Pixel;

			// Red bits
			*Pixel = 0;
			++Pixel;

			// Padding 
			*Pixel = 0;
			++Pixel;
			*/

			uint8 Blue = (X + BlueOffset);
			uint8 Green = (Y + GreenOffset);
			/*
				Memory:				BB GG RR xx
				Register:  		xx RR GG BB

			*/
			// Shift green and or blue to get 32 bit value since we Pixel pointer is uint32
			uint32 res = ((Green << 8) | Blue);
			*Pixel++ = res;

		}

		// Pointer arithimic, pretty much moving pointer to next row (memory is 1D but we think of it as 2D since its bitmap)
		Row += (RENDER_WIDTH * 4);
	}
	
}
// Side note, issue with fread before was that i was reading file with only
// "r" file mod and since target file can be just random bytes that have 0s,
// it would falsely detect EOF and return.
// I checked that with ieof
void HotLoadBuffer(win32_offscreen_buffer *Buffer)
{
	ifstream file(filePath, ios::binary | ios::ate);

	if (!file.is_open())
	{
		OutputDebugStringA("Unable to open file");
		return;
	}
	int size = file.tellg();
	file.seekg(0, ios::beg);
	int ourBufferLen = RENDER_HEIGHT * RENDER_WIDTH * 4;


	if (size < ourBufferLen)
	{
		char buff[200];
		sprintf(buff, "Wrong size. Size: %d\n", size);
		OutputDebugStringA(buff);
		return;
	}
	ShouldRender = false;
	file.read((char*)Buffer->Memory, ourBufferLen);
	file.close();
	
	ShouldRender = true;
}
win32_window_dimension Win32GetWindowDimension(HWND Window)
{
	win32_window_dimension Result;
	RECT ClientRect;
	GetClientRect(Window, &ClientRect);
	Result.Height = ClientRect.bottom - ClientRect.top;
	Result.Width = ClientRect.right - ClientRect.left;
	
	return Result;
}



void RenderText()
{
  long size;
  unsigned char* fontBuffer;
  FILE* fontFile = fopen("C:\\Windows\\Fonts\\arial.ttf", "rb");
  fseek(fontFile, 0, SEEK_END);
  size = ftell(fontFile);
  fseek(fontFile, 0, SEEK_SET);
  int fontSize = 36;
  fontBuffer = (unsigned char*)malloc(size);
  
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
  int bitmapWidth = RENDER_WIDTH;
  // this should be Ascent - Descent or something similar ?? basically max line height
  // but it font work for all fonts, so it may be something big that can just be reused
  // and read with boundaries
  int bitmapHeight = RENDER_HEIGHT;
  
  // this is Ascent - Descent
  int lineHeight = 16;

  unsigned char* bitmap = (unsigned char*)calloc(bitmapWidth * bitmapHeight, sizeof(unsigned char));

  // I think we should use this for scale?? or just value from Text line matrix or something similar
  float scaleFactor = stbtt_ScaleForPixelHeight(&info, lineHeight);

  char* textToTranslate = "D";
  int indexes[] = {4,8,11, 6, 1, 2, 10, 9, 7, 1, 5, 2, 3};
  char* textToTranslate2nd = "Second Row";
  vector<int> orderToDraw = {};
	vector<int> xPositions = {};
  int x =25; // look into why this makes printing more full 
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
  int useIndex = 0;
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
      stbtt_MakeGlyphBitmap(&info, (unsigned char*)GlobalBuffer.Memory + charOffset, c_x1 - c_x0, c_y1 - c_y0, bitmapWidth, scaleFactor, scaleFactor, indexes[i]);
    else
      stbtt_MakeCodepointBitmap(&info, bitmap + charOffset, c_x1 - c_x0, c_y1 - c_y0, bitmapWidth, scaleFactor, scaleFactor, textToTranslate[i], &orderToDraw);
    // advance x
    x += roundf(ax * scaleFactor);
		xPositions.push_back(x);
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
  ConvertBitmapToWindowsBitmap(&orderToDraw, &xPositions, bitmap);
  //  stbi_write_png("outMine.png", bitmapWidth, bitmapHeight, 1, bitmap, bitmapWidth);
  free(fontBuffer);
  free(bitmap);

}

void ConvertBitmapToWindowsBitmap(vector<int>* orderToDraw, vector<int>* xPositions, unsigned char *bitmap)
{
  int j =0;
  for (int i = 0; i < orderToDraw->size(); i++)
  {
    uint8 val = bitmap[i];
    uint8 valToPrint = 255 - val;
    //int x = xPositions->at(j);
    uint32 res = ((val << 16) | (val << 8) | val);
    int index = orderToDraw->at(i);
    ((uint32 *)GlobalBuffer.Memory)[index] = res;
    ShouldRender = true;
    Sleep(50);
    //j++;
  };

}