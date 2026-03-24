# Win7 Remote Control

Windows 7 远程控制工具，支持屏幕获取、鼠标/键盘控制、文件传输。

## 功能

- [x] 屏幕截图 (Session 0 隔离解决方案)
- [ ] 鼠标控制
- [ ] 键盘控制
- [ ] 拖放操作
- [ ] 文件传输

## 架构

```
┌─────────────┐     SSH/SMB      ┌──────────────────┐
│   Client    │ ────────────────▶ │ Windows Agent    │
│  (Mac/PC)   │                   │                  │
└─────────────┘                   │ - ScreenshotAgent│
      │                           │ - InputAgent     │
      │                           │ - FileAgent      │
      │                           └──────────────────┘
      │                                    ▲
      │                                    │
      └────────────────────────────────────┘
              CreateProcessAsUser
              (Session 1)
```

## 核心问题解决方案

Windows 7 Session 0 隔离导致服务进程无法访问用户桌面。本项目通过以下方式解决：

1. Windows 服务运行在 LocalSystem 账户
2. 使用 `WTSQueryUserToken` + `CreateProcessAsUser` 在用户会话中创建进程
3. 代理程序在用户桌面会话执行实际操作

## 开发

### 构建

```cmd
:: 编译截图代理
csc /target:exe /out:ScreenshotAgent.exe ScreenshotAgent.cs

:: 编译服务
csc /target:exe /out:ScreenshotServiceNew.exe /reference:System.ServiceProcess.dll ScreenshotServiceNew.cs
```

### 部署

```cmd
sc create Win7RC binPath= C:\Users\Public\Win7RC.exe
sc config Win7RC type= interact type= own
sc start Win7RC
```

## 协议

MIT License
