#include <stdio.h>
#include <tchar.h>
#include <Windows.h>
#include <string.h>
#include <fstream>
#include <iostream>
#include <thread>

using namespace std;
typedef uint8_t uint8;
typedef uint32_t uint32;
#define WIDTH 800
#define HEIGHT 600

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

			Win32CopyBufferToWindow(
				DeviceContext, WIDTH, HEIGHT,
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
  thread t(Observe);
  GlobalBuffer = {};
  // 4 is for bytes per pixel
  GlobalBuffer.Memory = VirtualAlloc(0, WIDTH*HEIGHT*4, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
  GlobalBuffer.Info = {};
  GlobalBuffer.Info.bmiHeader.biSize = sizeof(GlobalBuffer.Info.bmiHeader);
	GlobalBuffer.Info.bmiHeader.biWidth = WIDTH;
	// negative so bitmap is top to btottom and origin is upper left corner
	GlobalBuffer.Info.bmiHeader.biHeight = -HEIGHT;
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
	HotLoadBuffer(&GlobalBuffer);
	//RenderWeirdGradient(&GlobalBuffer, 0, 0);
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
        CW_USEDEFAULT,
        CW_USEDEFAULT,
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
					Win32CopyBufferToWindow(
						DeviceContext, Dimension.Width, Dimension.Height,
						&GlobalBuffer);	
			 }
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
      OutputDebugString("CHANGE");
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
	StretchDIBits(
		DeviceContext,
		/*
		X,Y,Width,Height,
		X,Y,Width,Height,
		*/
		0, 0, WindowWidth, WindowHeight,
		0, 0, WIDTH, HEIGHT,
		Buffer->Memory,
		&Buffer->Info,
		DIB_RGB_COLORS,
		SRCCOPY);
}

void RenderWeirdGradient(win32_offscreen_buffer *Buffer, int BlueOffset, int GreenOffset)
{
	// TODO lets see what o ptimized does
	// byte array pretty much
	uint8 *Row = (uint8 *)Buffer->Memory;
	for (int Y = 0; Y < HEIGHT; ++Y)
	{
		// uint8 *Pixel  = (uint8 *)Row;
		uint32 *Pixel = (uint32 *)Row;
		for (int X = 0; X < WIDTH; ++X)
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
		Row += (WIDTH * 4);
	}
	
}
void HotLoadBuffer(win32_offscreen_buffer *Buffer)
{
	ShouldRender = false;
	FILE* file = fopen(filePath, "r");
	if (file == NULL)
	{
		OutputDebugStringA("Unable to open file");
		return;
	}
	// just in case
	fseek(file, 0, SEEK_END);
	int size = ftell(file);
	fseek(file, 0, SEEK_SET);
	if (size != HEIGHT * WIDTH * 4)
	{
		OutputDebugStringA("WrongSizeOfFile");
		return;
	}

	fread(Buffer->Memory, 1, size, file);
	fclose(file);
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
