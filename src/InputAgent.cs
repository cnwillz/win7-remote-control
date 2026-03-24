using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

/// <summary>
/// 输入代理 - 处理鼠标和键盘事件
/// </summary>
class InputAgent
{
    [DllImport("user32.dll")]
    static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, uint dwExtraInfo);

    [DllImport("user32.dll")]
    static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

    [DllImport("user32.dll")]
    static extern short VkKeyScan(char ch);

    [DllImport("user32.dll")]
    static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    struct INPUT
    {
        public uint type;
        public INPUTUNION u;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct INPUTUNION
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
        [FieldOffset(0)]
        public KEYBDINPUT ki;
        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    // 鼠标标志
    const uint MOUSEEVENTF_MOVE = 0x0001;
    const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    const uint MOUSEEVENTF_LEFTUP = 0x0004;
    const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    const uint MOUSEEVENTF_WHEEL = 0x0800;
    const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

    // 键盘标志
    const uint KEYEVENTF_KEYDOWN = 0x0000;
    const uint KEYEVENTF_KEYUP = 0x0002;
    const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

    // 虚拟键码
    const byte VK_LBUTTON = 0x01;
    const byte VK_RBUTTON = 0x02;
    const byte VK_MBUTTON = 0x04;
    const byte VK_SHIFT = 0x10;
    const byte VK_CONTROL = 0x11;
    const byte VK_MENU = 0x12;  // ALT

    static string logFile = @"C:\Users\Public\InputAgent.log";

    static void Log(string msg)
    {
        try
        {
            Console.WriteLine("[InputAgent] " + msg);
            File.AppendAllText(logFile, DateTime.Now + " [InputAgent] " + msg + "\r\n");
        }
        catch { }
    }

    public static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("InputAgent <action> <params>");
            Console.WriteLine("  mouse move <x> <y>");
            Console.WriteLine("  mouse click <x> <y> [button]");
            Console.WriteLine("  mouse drag <x1> <y1> <x2> <y2> [button]");
            Console.WriteLine("  mouse wheel <delta>");
            Console.WriteLine("  key <vk> [extended]");
            Console.WriteLine("  text <string>");
            return;
        }

        string action = args[0].ToLower();
        Log("Action: " + action);

        try
        {
            switch (action)
            {
                case "mouse":
                    HandleMouse(args);
                    break;
                case "key":
                    HandleKey(args);
                    break;
                case "text":
                    HandleText(args);
                    break;
                default:
                    Log("Unknown action: " + action);
                    break;
            }
        }
        catch (Exception ex)
        {
            Log("Error: " + ex.Message);
        }
    }

    static void HandleMouse(string[] args)
    {
        if (args.Length < 2) return;
        string subAction = args[1].ToLower();

        switch (subAction)
        {
            case "move":
                if (args.Length >= 4)
                {
                    int x = int.Parse(args[2]);
                    int y = int.Parse(args[3]);
                    MouseMove(x, y);
                    Log(string.Format("Mouse moved to {0},{1}", x, y));
                }
                break;

            case "click":
                if (args.Length >= 4)
                {
                    int x = int.Parse(args[2]);
                    int y = int.Parse(args[3]);
                    string button = args.Length >= 5 ? args[4] : "left";
                    MouseClick(x, y, button);
                    Log(string.Format("Mouse clicked at {0},{1} with {2}", x, y, button));
                }
                break;

            case "drag":
                if (args.Length >= 6)
                {
                    int x1 = int.Parse(args[2]);
                    int y1 = int.Parse(args[3]);
                    int x2 = int.Parse(args[4]);
                    int y2 = int.Parse(args[5]);
                    string button = args.Length >= 7 ? args[6] : "left";
                    MouseDrag(x1, y1, x2, y2, button);
                    Log(string.Format("Mouse dragged from {0},{1} to {2},{3}", x1, y1, x2, y2));
                }
                break;

            case "wheel":
                if (args.Length >= 3)
                {
                    int delta = int.Parse(args[2]);
                    MouseWheel(delta);
                    Log("Mouse wheel: " + delta);
                }
                break;
        }
    }

    static void HandleKey(string[] args)
    {
        if (args.Length < 2) return;
        string vkStr = args[1];
        bool extended = args.Length >= 3 && args[2] == "1";

        // 解析虚拟键码
        ushort vk;
        if (vkStr.StartsWith("0x"))
            vk = ushort.Parse(vkStr.Substring(2), System.Globalization.NumberStyles.HexNumber);
        else if (vkStr.Length == 1)
            vk = (ushort)VkKeyScan(vkStr[0]);
        else
            vk = ushort.Parse(vkStr);

        KeyPress(vk, extended);
        Log("Key pressed: VK=" + vk);
    }

    static void HandleText(string[] args)
    {
        if (args.Length < 2) return;
        string text = args[1];
        TypeText(text);
        Log("Text typed: " + text);
    }

    public static void MouseMove(int x, int y)
    {
        SetCursorPos(x, y);
    }

    public static void MouseClick(int x, int y, string button)
    {
        SetCursorPos(x, y);
        Thread.Sleep(50);

        uint downFlag, upFlag;
        switch (button.ToLower())
        {
            case "right":
                downFlag = MOUSEEVENTF_RIGHTDOWN;
                upFlag = MOUSEEVENTF_RIGHTUP;
                break;
            case "middle":
                downFlag = MOUSEEVENTF_MIDDLEDOWN;
                upFlag = MOUSEEVENTF_MIDDLEUP;
                break;
            default:
                downFlag = MOUSEEVENTF_LEFTDOWN;
                upFlag = MOUSEEVENTF_LEFTUP;
                break;
        }

        mouse_event(downFlag, 0, 0, 0, 0);
        Thread.Sleep(50);
        mouse_event(upFlag, 0, 0, 0, 0);
    }

    public static void MouseDrag(int x1, int y1, int x2, int y2, string button)
    {
        // 移动到起点
        SetCursorPos(x1, y1);
        Thread.Sleep(50);

        // 按下
        uint downFlag;
        switch (button.ToLower())
        {
            case "right":
                downFlag = MOUSEEVENTF_RIGHTDOWN;
                break;
            case "middle":
                downFlag = MOUSEEVENTF_MIDDLEDOWN;
                break;
            default:
                downFlag = MOUSEEVENTF_LEFTDOWN;
                break;
        }
        mouse_event(downFlag, 0, 0, 0, 0);
        Thread.Sleep(100);

        // 拖动到终点 (分步移动避免太快)
        int steps = Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1)) / 10;
        if (steps < 1) steps = 1;
        for (int i = 1; i <= steps; i++)
        {
            int cx = x1 + (x2 - x1) * i / steps;
            int cy = y1 + (y2 - y1) * i / steps;
            SetCursorPos(cx, cy);
            Thread.Sleep(10);
        }
        Thread.Sleep(50);

        // 松开
        uint upFlag;
        switch (button.ToLower())
        {
            case "right":
                upFlag = MOUSEEVENTF_RIGHTUP;
                break;
            case "middle":
                upFlag = MOUSEEVENTF_MIDDLEUP;
                break;
            default:
                upFlag = MOUSEEVENTF_LEFTUP;
                break;
        }
        mouse_event(upFlag, 0, 0, 0, 0);
    }

    public static void MouseWheel(int delta)
    {
        mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)delta, 0);
    }

    public static void KeyPress(ushort vk, bool extended = false)
    {
        uint flags = KEYEVENTF_KEYDOWN | (extended ? KEYEVENTF_EXTENDEDKEY : 0);
        keybd_event((byte)vk, 0, flags, 0);
        Thread.Sleep(50);
        keybd_event((byte)vk, 0, KEYEVENTF_KEYUP | (extended ? KEYEVENTF_EXTENDEDKEY : 0), 0);
    }

    public static void TypeText(string text)
    {
        foreach (char c in text)
        {
            short vkScan = VkKeyScan(c);
            byte vk = (byte)(vkScan & 0xff);
            byte scan = (byte)(vkScan >> 8);

            // 检查 Shift 状态
            bool shift = (vkScan & 0x100) != 0;
            if (shift)
            {
                KeyPress(VK_SHIFT);
            }

            // 发送按键
            uint flags = KEYEVENTF_KEYDOWN;
            keybd_event(vk, (byte)scan, flags, 0);
            Thread.Sleep(30);
            keybd_event(vk, (byte)scan, KEYEVENTF_KEYUP, 0);

            if (shift)
            {
                KeyPress(VK_SHIFT);
            }

            Thread.Sleep(30);
        }
    }

    // 组合键辅助方法
    public static void SendCtrl(char c)
    {
        KeyPress(VK_CONTROL);
        short vkScan = VkKeyScan(c);
        byte vk = (byte)(vkScan & 0xff);
        byte scan = (byte)(vkScan >> 8);
        keybd_event(vk, scan, KEYEVENTF_KEYDOWN, 0);
        Thread.Sleep(30);
        keybd_event(vk, scan, KEYEVENTF_KEYUP, 0);
        Thread.Sleep(30);
        KeyPress(VK_CONTROL);
    }

    public static void SendAlt(char c)
    {
        KeyPress(VK_MENU);
        short vkScan = VkKeyScan(c);
        byte vk = (byte)(vkScan & 0xff);
        byte scan = (byte)(vkScan >> 8);
        keybd_event(vk, scan, KEYEVENTF_KEYDOWN, 0);
        Thread.Sleep(30);
        keybd_event(vk, scan, KEYEVENTF_KEYUP, 0);
        Thread.Sleep(30);
        KeyPress(VK_MENU);
    }
}
