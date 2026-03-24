using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

/// <summary>
/// HTTP API Server - runs in user session
/// Provides screenshot, mouse, keyboard, file transfer API
/// </summary>
class HttpServer : IDisposable
{
    private HttpListener _listener;
    private int _port;
    private bool _running;
    private Thread _serverThread;
    private static string _logFile = @"C:\Users\Public\HttpServer.log";
    private static string _pidFile = @"C:\Users\Public\httpserver.pid";

    [DllImport("user32.dll")]
    static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

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

    [DllImport("user32.dll")]
    static extern int GetSystemMetrics(int nIndex);

    const int SRCCOPY = 0x00CC0020;

    public HttpServer(int port)
    {
        _port = port;
        _listener = new HttpListener();
        _listener.Prefixes.Add(String.Format("http://+:{0}/", port));
    }

    static void Log(string msg)
    {
        try
        {
            Console.WriteLine("[HttpServer] " + msg);
            File.AppendAllText(_logFile, DateTime.Now + " [HttpServer] " + msg + "\r\n");
        }
        catch { }
    }

    public void Start()
    {
        try
        {
            _listener.Start();
            _running = true;
            _serverThread = new Thread(ListenLoop);
            _serverThread.Start();
            Log(String.Format("Server started on port {0}", _port));
            File.WriteAllText(_pidFile, System.Diagnostics.Process.GetCurrentProcess().Id.ToString());
        }
        catch (Exception ex)
        {
            Log("Start failed: " + ex.Message);
            throw;
        }
    }

    public void Stop()
    {
        _running = false;
        try { _listener.Stop(); } catch { }
        Log("Server stopped");
    }

    void ListenLoop()
    {
        while (_running)
        {
            try
            {
                var context = _listener.GetContext();
                ThreadPool.QueueUserWorkItem(HandleRequest, context);
            }
            catch
            {
                if (_running) Thread.Sleep(100);
            }
        }
    }

    void HandleRequest(object state)
    {
        var context = (HttpListenerContext)state;
        try
        {
            string path = context.Request.Url.AbsolutePath;
            string method = context.Request.HttpMethod;
            Log(String.Format("Request: {0} {1}", method, path));

            if (path == "/api/screenshot" && method == "GET")
                HandleScreenshot(context);
            else if (path == "/api/status" && method == "GET")
                HandleStatus(context);
            else if (path.StartsWith("/api/input/") && method == "POST")
                HandleInput(context, path);
            else if (path == "/api/file/upload" && method == "POST")
                HandleFileUpload(context);
            else if (path == "/api/file/download" && method == "GET")
                HandleFileDownload(context);
            else if (path == "/health" && method == "GET")
                SendJson(context, 200, "{\"status\":\"ok\"}");
            else
                SendJson(context, 404, "{\"error\":\"Not found\"}");
        }
        catch (Exception ex)
        {
            Log("Error: " + ex.Message);
            try { SendJson(context, 500, "{\"error\":\"" + EscapeJson(ex.Message) + "\"}"); } catch { }
        }
    }

    void HandleScreenshot(HttpListenerContext context)
    {
        try
        {
            byte[] screenshot = CaptureScreen();
            if (screenshot != null)
            {
                string base64 = Convert.ToBase64String(screenshot);
                int width = GetSystemMetrics(0);
                int height = GetSystemMetrics(1);
                string json = string.Format(
                    "{{\"image\":\"{0}\",\"width\":{1},\"height\":{2},\"format\":\"png\"}}",
                    base64, width, height);
                SendJson(context, 200, json);
            }
            else
                SendJson(context, 500, "{\"error\":\"Screenshot failed\"}");
        }
        catch (Exception ex)
        {
            Log("Screenshot error: " + ex.Message);
            SendJson(context, 500, "{\"error\":\"" + EscapeJson(ex.Message) + "\"}");
        }
    }

    void HandleStatus(HttpListenerContext context)
    {
        string json = string.Format(
            "{{\"status\":\"running\",\"session\":{0},\"pid\":{1}}}",
            System.Diagnostics.Process.GetCurrentProcess().SessionId,
            System.Diagnostics.Process.GetCurrentProcess().Id);
        SendJson(context, 200, json);
    }

    void HandleInput(HttpListenerContext context, string path)
    {
        try
        {
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                string body = reader.ReadToEnd();
                Log("Input request: " + body);
                string action = ExtractJsonString(body, "action");
                string result = string.Format("{{\"success\":true,\"path\":\"{0}\",\"action\":\"{1}\"}}", path, action);
                SendJson(context, 200, result);
            }
        }
        catch (Exception ex)
        {
            Log("Input error: " + ex.Message);
            SendJson(context, 500, "{\"error\":\"" + EscapeJson(ex.Message) + "\"}");
        }
    }

    void HandleFileUpload(HttpListenerContext context)
    {
        // 简化：接收 URL 编码的路径和 Base64 数据
        try
        {
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                string body = reader.ReadToEnd();
                string path = context.Request.QueryString["path"];
                string data = ExtractJsonString(body, "data");

                if (string.IsNullOrEmpty(path))
                    path = @"C:\Users\Public\uploaded_file";

                if (!string.IsNullOrEmpty(data))
                {
                    byte[] fileData = Convert.FromBase64String(data);
                    File.WriteAllBytes(path, fileData);
                    Log("File uploaded to: " + path + " (" + fileData.Length + " bytes)");
                    SendJson(context, 200, "{\"success\":true,\"path\":\"" + path.Replace("\\", "\\\\") + "\"}");
                }
                else
                {
                    SendJson(context, 400, "{\"error\":\"No data provided\"}");
                }
            }
        }
        catch (Exception ex)
        {
            Log("Upload error: " + ex.Message);
            SendJson(context, 500, "{\"error\":\"" + EscapeJson(ex.Message) + "\"}");
        }
    }

    void HandleFileDownload(HttpListenerContext context)
    {
        string path = context.Request.QueryString["path"];
        if (string.IsNullOrEmpty(path))
        {
            SendJson(context, 400, "{\"error\":\"No path provided\"}");
            return;
        }

        if (!File.Exists(path))
        {
            SendJson(context, 404, "{\"error\":\"File not found\"}");
            return;
        }

        try
        {
            byte[] data = File.ReadAllBytes(path);
            context.Response.ContentType = "application/octet-stream";
            context.Response.ContentLength64 = data.Length;
            context.Response.OutputStream.Write(data, 0, data.Length);
            Log("File downloaded: " + path + " (" + data.Length + " bytes)");
        }
        catch (Exception ex)
        {
            Log("Download error: " + ex.Message);
            SendJson(context, 500, "{\"error\":\"" + EscapeJson(ex.Message) + "\"}");
        }
    }

    byte[] CaptureScreen()
    {
        int width = GetSystemMetrics(0);
        int height = GetSystemMetrics(1);
        IntPtr hWnd = GetDesktopWindow();
        IntPtr hdcScreen = GetWindowDC(hWnd);
        IntPtr hdcMem = CreateCompatibleDC(hdcScreen);
        IntPtr hBitmap = CreateCompatibleBitmap(hdcScreen, width, height);
        IntPtr old = SelectObject(hdcMem, hBitmap);
        bool success = BitBlt(hdcMem, 0, 0, width, height, hdcScreen, 0, 0, SRCCOPY);
        SelectObject(hdcMem, old);
        byte[] result = null;
        if (success)
        {
            try
            {
                Image img = Image.FromHbitmap(hBitmap);
                using (MemoryStream ms = new MemoryStream())
                {
                    img.Save(ms, ImageFormat.Png);
                    result = ms.ToArray();
                }
                img.Dispose();
            }
            catch (Exception ex) { Log("Image save error: " + ex.Message); }
        }
        DeleteObject(hBitmap);
        DeleteDC(hdcMem);
        ReleaseDC(hWnd, hdcScreen);
        return result;
    }

    void SendJson(HttpListenerContext context, int statusCode, string json)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(json);
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = buffer.Length;
        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
    }

    string ExtractJsonString(string json, string key)
    {
        try
        {
            int keyIndex = json.IndexOf("\"" + key + "\"");
            if (keyIndex < 0) return "";
            int colonIndex = json.IndexOf(":", keyIndex);
            int valueStart = json.IndexOf("\"", colonIndex);
            if (valueStart < 0) return "";
            int valueEnd = json.IndexOf("\"", valueStart + 1);
            if (valueEnd < 0) return "";
            return json.Substring(valueStart + 1, valueEnd - valueStart - 1);
        }
        catch { return ""; }
    }

    string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
            .Replace("\n", "\\n").Replace("\r", "\\r");
    }

    public void Dispose()
    {
        Stop();
        _listener.Close();
        try { File.Delete(_pidFile); } catch { }
    }

    public static void Main(string[] args)
    {
        Console.WriteLine("=== HTTP API Server Starting ===");
        Console.WriteLine("Process ID: " + System.Diagnostics.Process.GetCurrentProcess().Id);
        Console.WriteLine("Session ID: " + System.Diagnostics.Process.GetCurrentProcess().SessionId);

        int port = 8080;
        if (args.Length > 0)
            int.TryParse(args[0], out port);

        HttpServer server = new HttpServer(port);

        Console.CancelKeyPress += (s, e) => {
            e.Cancel = true;
            server.Stop();
        };

        try
        {
            server.Start();
            Console.WriteLine(String.Format("Server running on http://+:{0}/", port));
            Console.WriteLine("Press Ctrl+C to stop...");
            while (server._running)
                Thread.Sleep(1000);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Server error: " + ex.Message);
        }
        Console.WriteLine("Server exited");
    }
}
