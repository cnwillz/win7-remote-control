using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;

class ScreenshotAgent
{
    [DllImport("user32.dll")]
    static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll")]
    static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

    [DllImport("gdi32.dll")]
    static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest, IntPtr hdcSrc, int xSrc, int ySrc, int rop);

    [DllImport("gdi32.dll")]
    static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    static extern IntPtr SelectObject(IntPtr hdc, IntPtr hObject);

    [DllImport("gdi32.dll")]
    static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    static extern bool DeleteObject(IntPtr hObject);

    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("kernel32.dll")]
    static extern int GetLastError();

    const int SRCCOPY = 0x00CC0020;
    const uint PW_CLIENTONLY = 1;
    const uint PW_RENDERFULLCONTENT = 2;

    static string logFile = @"C:\Users\Public\ScreenshotAgent.log";

    static void Log(string msg)
    {
        try
        {
            Console.WriteLine(msg);
            File.AppendAllText(logFile, DateTime.Now + ": " + msg + "\r\n");
        }
        catch { }
    }

    static void Main(string[] args)
    {
        Log("ScreenshotAgent starting...");
        Log("Process ID: " + System.Diagnostics.Process.GetCurrentProcess().Id);
        Log("Session ID: " + System.Diagnostics.Process.GetCurrentProcess().SessionId);

        string outputFile = args.Length > 0 ? args[0] : @"C:\Users\Public\screenshot.png";

        // 等待一下确保桌面完全加载
        Thread.Sleep(1000);

        if (CaptureScreen(outputFile))
        {
            Log("Screenshot captured: " + outputFile);
            FileInfo fi = new FileInfo(outputFile);
            Log("File size: " + fi.Length + " bytes");
        }
        else
        {
            Log("Screenshot failed!");
        }

        Log("ScreenshotAgent exiting...");
    }

    static bool CaptureScreen(string filename)
    {
        Log("CaptureScreen called with: " + filename);

        IntPtr hWnd = GetDesktopWindow();
        Log("Desktop window: " + hWnd);

        // 获取屏幕尺寸
        int width = GetSystemMetrics(0);  // SM_CXSCREEN
        int height = GetSystemMetrics(1);  // SM_CYSCREEN
        Log("Screen size: " + width + "x" + height);

        IntPtr hdcScreen = GetWindowDC(hWnd);
        IntPtr hdcMem = CreateCompatibleDC(hdcScreen);

        IntPtr hBitmap = CreateCompatibleBitmap(hdcScreen, width, height);
        IntPtr old = SelectObject(hdcMem, hBitmap);

        Log("Before PrintWindow");
        bool success = PrintWindow(hWnd, hdcMem, PW_RENDERFULLCONTENT);
        Log("PrintWindow result: " + success);

        if (!success)
        {
            // 尝试 PW_CLIENTONLY
            success = PrintWindow(hWnd, hdcMem, PW_CLIENTONLY);
            Log("PrintWindow(PW_CLIENTONLY) result: " + success);
        }

        if (!success)
        {
            // 最后尝试 BitBlt
            success = BitBlt(hdcMem, 0, 0, width, height, hdcScreen, 0, 0, SRCCOPY);
            Log("BitBlt result: " + success);
        }

        SelectObject(hdcMem, old);

        if (success)
        {
            try
            {
                Image img = Image.FromHbitmap(hBitmap);
                img.Save(filename, ImageFormat.Png);
                img.Dispose();
                Log("Image saved successfully");
            }
            catch (Exception ex)
            {
                Log("Save error: " + ex.Message);
                success = false;
            }
        }

        DeleteObject(hBitmap);
        DeleteDC(hdcMem);
        ReleaseDC(hWnd, hdcScreen);

        return success;
    }

    [DllImport("user32.dll")]
    static extern int GetSystemMetrics(int nIndex);
}
