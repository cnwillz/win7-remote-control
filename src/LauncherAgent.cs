using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;

namespace Win7RC
{
    public class LauncherService : ServiceBase
    {
        [DllImport("kernel32.dll")]
        static extern int WTSGetActiveConsoleSessionId();

        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern bool WTSQueryUserToken(int SessionId, out IntPtr phToken);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool CreateProcessAsUser(IntPtr hToken, string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, int dwCreationFlags, string lpEnvironment, string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("userenv.dll", SetLastError = true)]
        static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

        [DllImport("userenv.dll", SetLastError = true)]
        static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        static extern int GetLastError();

        [StructLayout(LayoutKind.Sequential)]
        struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        const int CREATE_NO_WINDOW = 0x08000000;
        const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        const int NORMAL_PRIORITY_CLASS = 0x00000020;

        private static string _logFile = @"C:\Users\Public\LauncherService.log";
        private Thread _workerThread;
        private bool _stopRequested;
        private int _httpServerPid = 0;

        public LauncherService()
        {
            ServiceName = "Win7RCHttp";
            CanStop = true;
            CanShutdown = true;
        }

        static void Log(string msg)
        {
            try
            {
                Console.WriteLine("[Launcher] " + msg);
                File.AppendAllText(_logFile, DateTime.Now + " [Launcher] " + msg + "\r\n");
            }
            catch { }
        }

        protected override void OnStart(string[] args)
        {
            Log("=== Launcher Service Starting ===");
            Log("Process ID: " + Process.GetCurrentProcess().Id);
            Log("Session ID: " + Process.GetCurrentProcess().SessionId);

            _stopRequested = false;
            _workerThread = new Thread(ServiceWorker);
            _workerThread.Start();
        }

        protected override void OnStop()
        {
            Log("=== Launcher Service Stopping ===");

            // 终止 HTTP 服务器
            if (_httpServerPid > 0)
            {
                try
                {
                    Process p = Process.GetProcessById(_httpServerPid);
                    p.Kill();
                    Log("HTTP Server (PID " + _httpServerPid + ") terminated");
                }
                catch { }
            }

            _stopRequested = true;
            if (_workerThread != null && _workerThread.IsAlive)
                _workerThread.Join(5000);
        }

        void ServiceWorker()
        {
            Log("Service worker started");

            // 启动 HTTP 服务器
            StartHttpServer();

            while (!_stopRequested)
            {
                Thread.Sleep(1000);

                // 检查 HTTP 服务器是否还在运行
                if (_httpServerPid > 0)
                {
                    try
                    {
                        Process.GetProcessById(_httpServerPid);
                    }
                    catch
                    {
                        Log("HTTP Server died, restarting...");
                        StartHttpServer();
                    }
                }
            }

            Log("Service worker exiting");
        }

        void StartHttpServer()
        {
            string httpServerExe = @"C:\Users\Public\HttpServer.exe";
            if (!File.Exists(httpServerExe))
            {
                Log("HttpServer.exe not found!");
                return;
            }

            int sessionId = WTSGetActiveConsoleSessionId();
            Log("Active console session: " + sessionId);

            if (sessionId == -1)
            {
                Log("No active console session");
                return;
            }

            IntPtr hToken;
            if (!WTSQueryUserToken(sessionId, out hToken))
            {
                Log("WTSQueryUserToken failed: " + GetLastError());
                return;
            }
            Log("Got user token: " + hToken);

            IntPtr pEnv;
            if (!CreateEnvironmentBlock(out pEnv, hToken, false))
            {
                Log("CreateEnvironmentBlock failed: " + GetLastError());
                CloseHandle(hToken);
                return;
            }

            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            si.lpDesktop = @"winsta0\default";
            si.wShowWindow = 1;  // SW_SHOWNORMAL

            string cmdLine = "\"" + httpServerExe + "\" 8080";
            int dwCreationFlags = NORMAL_PRIORITY_CLASS | CREATE_NO_WINDOW | CREATE_UNICODE_ENVIRONMENT;

            PROCESS_INFORMATION pi;
            if (!CreateProcessAsUser(hToken, httpServerExe, cmdLine, IntPtr.Zero, IntPtr.Zero, false, dwCreationFlags, null, null, ref si, out pi))
            {
                Log("CreateProcessAsUser failed: " + GetLastError());
                DestroyEnvironmentBlock(pEnv);
                CloseHandle(hToken);
                return;
            }

            _httpServerPid = pi.dwProcessId;
            Log("HTTP Server started with PID: " + _httpServerPid);

            DestroyEnvironmentBlock(pEnv);
            CloseHandle(pi.hProcess);
            CloseHandle(pi.hThread);
            CloseHandle(hToken);
        }

        public static void Main()
        {
            ServiceBase[] servicesToRun = new ServiceBase[] { new LauncherService() };
            ServiceBase.Run(servicesToRun);
        }
    }
}
