using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.IO;

namespace ScreenshotService
{
    public class ScreenshotSvc : ServiceBase
    {
        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        static extern int GetLastError();

        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern bool WTSEnumerateSessions(IntPtr hServer, int Reserved, int Version, out IntPtr pSessionInfo, out int pCount);

        [DllImport("wtsapi32.dll")]
        static extern void WTSFreeMemory(IntPtr pSessionInfo);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern bool WTSQueryUserToken(int SessionId, out IntPtr phToken);

        [DllImport("wtsapi32.dll")]
        static extern int WTSGetActiveConsoleSessionId();

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool CreateProcessAsUser(IntPtr hToken, string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, int dwCreationFlags, string lpEnvironment, string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("userenv.dll", SetLastError = true)]
        static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

        [DllImport("userenv.dll", SetLastError = true)]
        static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

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

        [StructLayout(LayoutKind.Sequential)]
        struct WTS_SESSION_INFO
        {
            public int SessionId;
            public string pWinStationName;
            public int State;
        }

        const int WTS_ACTIVE = 0;
        const int CREATE_NO_WINDOW = 0x08000000;
        const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        const int NORMAL_PRIORITY_CLASS = 0x00000020;

        private static string logFile = @"C:\Users\Public\ScreenshotServiceNew.log";
        private static string requestFile = @"C:\Users\Public\request_screenshot.txt";
        private static string outputFile = @"C:\Users\Public\service_screenshot.png";
        private Thread workerThread;
        private bool stopRequested = false;

        public ScreenshotSvc()
        {
            ServiceName = "ScreenshotSvcNew";
            CanStop = true;
            CanShutdown = true;
        }

        static void Log(string msg)
        {
            try
            {
                Console.WriteLine(msg);
                File.AppendAllText(logFile, DateTime.Now + ": " + msg + "\r\n");
            }
            catch { }
        }

        protected override void OnStart(string[] args)
        {
            Log("=== ScreenshotSvcNew Starting ===");
            Log("Process ID: " + Process.GetCurrentProcess().Id);
            Log("Session ID: " + Process.GetCurrentProcess().SessionId);

            stopRequested = false;
            workerThread = new Thread(ServiceWorker);
            workerThread.Start();
        }

        protected override void OnStop()
        {
            Log("=== ScreenshotSvcNew Stopping ===");
            stopRequested = true;
            if (workerThread != null && workerThread.IsAlive)
            {
                workerThread.Join(5000);
            }
        }

        void ServiceWorker()
        {
            Log("Service worker started");

            while (!stopRequested)
            {
                try
                {
                    if (File.Exists(requestFile))
                    {
                        Log("Screenshot request detected!");
                        File.Delete(requestFile);

                        string agentExe = @"C:\Users\Public\ScreenshotAgent.exe";
                        if (File.Exists(agentExe))
                        {
                            if (LaunchAgentInUserSession(agentExe, outputFile))
                            {
                                Log("Screenshot captured successfully!");
                            }
                            else
                            {
                                Log("Screenshot capture failed");
                            }
                        }
                        else
                        {
                            Log("ScreenshotAgent.exe not found");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log("Error: " + ex.Message);
                }

                Thread.Sleep(1000);
            }

            Log("Service worker exiting");
        }

        bool LaunchAgentInUserSession(string exePath, string outputFile)
        {
            Log("LaunchAgentInUserSession called");
            int sessionId = WTSGetActiveConsoleSessionId();
            Log("Active console session: " + sessionId);

            if (sessionId == -1)
            {
                sessionId = FindActiveSession();
                Log("Fallback to session: " + sessionId);
            }

            if (sessionId == -1)
            {
                Log("No active session found");
                return false;
            }

            IntPtr hToken;
            if (!WTSQueryUserToken(sessionId, out hToken))
            {
                int err = GetLastError();
                Log("WTSQueryUserToken failed: " + err);
                return false;
            }
            Log("Got user token: " + hToken);

            IntPtr pEnv;
            if (!CreateEnvironmentBlock(out pEnv, hToken, false))
            {
                int err = GetLastError();
                Log("CreateEnvironmentBlock failed: " + err);
                CloseHandle(hToken);
                return false;
            }

            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            si.lpDesktop = @"winsta0\default";
            si.wShowWindow = 0;

            string cmdLine = "\"" + exePath + "\" \"" + outputFile + "\"";
            int dwCreationFlags = NORMAL_PRIORITY_CLASS | CREATE_NO_WINDOW | CREATE_UNICODE_ENVIRONMENT;

            PROCESS_INFORMATION pi;
            Log("Calling CreateProcessAsUser...");

            if (!CreateProcessAsUser(hToken, exePath, cmdLine, IntPtr.Zero, IntPtr.Zero, false, dwCreationFlags, null, null, ref si, out pi))
            {
                int err = GetLastError();
                Log("CreateProcessAsUser failed: " + err);

                // 尝试不使用 CREATE_UNICODE_ENVIRONMENT
                if (!CreateProcessAsUser(hToken, exePath, cmdLine, IntPtr.Zero, IntPtr.Zero, false, dwCreationFlags & ~CREATE_UNICODE_ENVIRONMENT, null, null, ref si, out pi))
                {
                    err = GetLastError();
                    Log("CreateProcessAsUser (no unicode) failed: " + err);
                    DestroyEnvironmentBlock(pEnv);
                    CloseHandle(hToken);
                    return false;
                }
            }

            Log("Process created: " + pi.dwProcessId);

            DestroyEnvironmentBlock(pEnv);
            CloseHandle(pi.hProcess);
            CloseHandle(pi.hThread);
            CloseHandle(hToken);

            Thread.Sleep(2000);
            return File.Exists(outputFile);
        }

        int FindActiveSession()
        {
            IntPtr pSessionInfo = IntPtr.Zero;
            int pCount = 0;

            try
            {
                if (!WTSEnumerateSessions(IntPtr.Zero, 0, 1, out pSessionInfo, out pCount))
                {
                    return -1;
                }

                for (int i = 0; i < pCount; i++)
                {
                    IntPtr iter = pSessionInfo + i * Marshal.SizeOf(typeof(WTS_SESSION_INFO));
                    WTS_SESSION_INFO si = (WTS_SESSION_INFO)Marshal.PtrToStructure(iter, typeof(WTS_SESSION_INFO));

                    Log("Session " + i + ": ID=" + si.SessionId + ", State=" + si.State + ", Name=" + si.pWinStationName);

                    if (si.State == WTS_ACTIVE && si.SessionId > 0)
                    {
                        return si.SessionId;
                    }
                }
            }
            finally
            {
                if (pSessionInfo != IntPtr.Zero)
                    WTSFreeMemory(pSessionInfo);
            }

            return -1;
        }

        public static void Main()
        {
            ServiceBase[] servicesToRun = new ServiceBase[] { new ScreenshotSvc() };
            ServiceBase.Run(servicesToRun);
        }
    }
}
