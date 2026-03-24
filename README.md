# Win7 Remote Control

Windows 7 远程控制工具，通过 HTTP API 提供屏幕获取、鼠标/键盘控制、文件传输功能。

## 功能

| 功能 | 状态 | 说明 |
|------|------|------|
| 屏幕截图 | ✅ | 支持 PNG/JPEG, 质量/缩放可调 |
| 鼠标控制 | ✅ | 移动/点击/拖动/滚轮 |
| 键盘控制 | ✅ | 按键/文本输入 |
| 文件传输 | ✅ | Base64 上传/下载 |
| HTTP API | ✅ | RESTful 接口 |

**测试结果: 12/12 API 测试通过**

## 快速开始

### 1. 部署

```bash
# SMB 挂载
./scripts/smb_mount.sh mount

# 完整部署 (上传 + 编译 + 重启服务 + 测试)
./scripts/deploy.sh
```

### 2. 使用 API

```python
from client.win7_remote import Win7Remote

client = Win7Remote(host="192.168.5.55", port=8080)

# 截图
img = client.screenshot(format="jpeg", quality=70)

# 鼠标点击
client.mouse_click(500, 300)

# 文本输入
client.keyboard_text("hello world")
```

### 3. 测试

```bash
# 完整测试
./scripts/api_test.py --test all

# 单项测试
./scripts/api_test.py --test screenshot
./scripts/api_test.py --test input
```

## 系统架构

```
┌─────────────────────────────────────────────────────────┐
│  Win7 Remote Control                                    │
│                                                          │
│  ┌─────────────┐     ┌──────────────────────────────┐   │
│  │ Win7RCHttp │     │      HttpServer.exe          │   │
│  │  Service   │────▶│  (Session 1 - User Desktop)  │   │
│  │ (Session 0)│     │  ├── Screenshot (GDI BitBlt)│   │
│  └─────────────┘     │  ├── Mouse/Keyboard Input    │   │
│        │             │  │    via InputAgent.exe     │   │
│        │             │  └── File Transfer            │   │
│        │             └──────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

## API 端点

| 端点 | 方法 | 说明 |
|------|------|------|
| `/api/screenshot` | GET | 获取屏幕截图 |
| `/api/status` | GET | 获取状态信息 |
| `/api/input/mouse` | POST | 鼠标操作 |
| `/api/input/keyboard` | POST | 键盘按键 |
| `/api/input/text` | POST | 文本输入 |
| `/api/file/upload` | POST | 文件上传 |
| `/api/file/download` | GET | 文件下载 |
| `/health` | GET | 健康检查 |

## Session 0 隔离解决方案

Windows 7 将服务(Session 0)和用户桌面(Session 1)隔离。通过 `Win7RCHttp` Windows 服务使用 `CreateProcessAsUser` 在用户桌面启动 `HttpServer`。

```cmd
:: 安装服务 (只需一次)
sc create Win7RCHttp binPath= C:\Users\Public\LauncherAgent.exe
sc config Win7RCHttp type= interact type= own
sc start Win7RCHttp
```

## 项目结构

```
win7-remote-control/
├── src/
│   ├── HttpServer.cs      # HTTP API 服务器
│   ├── InputAgent.cs      # 输入控制代理
│   └── LauncherAgent.cs   # Windows 服务
├── client/
│   └── win7_remote.py    # Python SDK
├── scripts/
│   ├── smb_mount.sh      # SMB 挂载
│   ├── ssh_remote.py     # 远程控制
│   ├── api_test.py       # API 测试
│   └── deploy.sh         # 部署脚本
├── docs/
│   └── PLAN.md           # 开发计划
└── README.md
```

## 开发

### 编译

```cmd
:: HttpServer
C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe /target:exe /out:C:\Users\Public\HttpServer.exe C:\Users\Public\HttpServer.cs

:: InputAgent
C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe /target:exe /out:C:\Users\Public\InputAgent.exe C:\Users\Public\InputAgent.cs
```

### 远程控制

```bash
# 查看状态
./scripts/ssh_remote.py status --service Win7RCHttp

# 启动服务
./scripts/ssh_remote.py service-start

# 停止服务
./scripts/ssh_remote.py service-stop

# 查看日志
./scripts/ssh_remote.py log --log-lines 20
```

## 协议

MIT License
