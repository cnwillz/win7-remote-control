# Win7 Remote Control - 开发计划

## 目标
将截图工具升级为可用的远程控制程序，通过 HTTP API 从本机调用，实现完整的远程控制功能。

## 功能状态

### Phase 1: 基础能力 (MVP) ✅ 完成
- [x] **屏幕获取** - GDI BitBlt, 支持 PNG/JPEG, 质量/缩放可调
- [x] **HTTP API** - HttpListener 实现
- [x] **命令行工具** - Python SDK + 测试脚本

### Phase 2: 输入控制 ✅ 完成
- [x] **鼠标控制** - 移动/点击/拖动/滚轮
- [x] **键盘控制** - 按键/文本输入
- [ ] **组合键支持** - Ctrl+C, Alt+Tab 等 (部分支持)

### Phase 3: 文件传输 ✅ 完成
- [x] **上传下载** - Base64 方式实现

### Phase 4: 高级功能 (待开发)
- [ ] **剪贴板同步** - 跨设备复制粘贴
- [ ] **多屏支持** - 多显示器环境
- [ ] **拖放操作** - 文件跨网络拖放

## 技术方案

### 通信协议: HTTP REST API
- 简单易用
- 易于调试
- 延迟可接受

### Agent 架构

```
Win7 Remote Control/
├── LauncherAgent.cs       # Windows 服务 (Session 0 → 用户 Session)
├── HttpServer.cs         # HTTP API 服务器 (用户 Session)
├── InputAgent.cs         # 输入控制代理
└── SessionManager.cs     # 会话管理
```

### API 设计

| 端点 | 方法 | 状态 |
|------|------|------|
| `/api/screenshot` | GET | ✅ |
| `/api/status` | GET | ✅ |
| `/api/input/mouse` | POST | ✅ |
| `/api/input/keyboard` | POST | ✅ |
| `/api/input/text` | POST | ✅ |
| `/api/file/upload` | POST | ✅ |
| `/api/file/download` | GET | ✅ |
| `/health` | GET | ✅ |

## 已完成任务

| 日期 | 任务 | 说明 |
|------|------|------|
| 2026-03-25 | Session 0 隔离解决 | 通过 Win7RCHttp 服务 + CreateProcessAsUser |
| 2026-03-25 | HTTP API 实现 | HttpServer.cs + 8 个端点 |
| 2026-03-25 | 鼠标键盘控制 | InputAgent.cs + ExecuteInputAgent |
| 2026-03-25 | ExtractJsonString 修复 | 支持未加引号整数 JSON |
| 2026-03-25 | Python SDK | client/win7_remote.py |
| 2026-03-25 | 部署脚本 | scripts/ 目录 |
| 2026-03-25 | API 测试通过 | 12/12 测试 |

## 待优化

1. **截图压缩** - JPEG 70% 60KB, 可进一步优化
2. **组合键支持** - 修饰键组合未完全测试
3. **多显示器** - 仅支持主显示器
4. **错误处理** - 完善各端点错误处理
5. **日志系统** - 统一日志管理

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
│   └── PLAN.md           # 本文件
└── README.md
```

## 使用方式

```bash
# 1. 部署
./scripts/deploy.sh

# 2. 测试
./scripts/api_test.py --test all

# 3. 使用 Python SDK
python3 -c "
from client.win7_remote import Win7Remote
c = Win7Remote('192.168.5.55', 8080)
c.screenshot().save('/tmp/screen.jpg')
c.mouse_click(500, 300)
c.keyboard_text('hello')
"
```
