# Win7 Remote Control - 开发计划

## 目标
将截图工具升级为可用的远程控制程序，通过 API 从本机调用，实现完整的远程控制功能。

## 功能列表

### Phase 1: 基础能力 (MVP)
- [ ] **屏幕获取** - 已实现，需优化
- [ ] **API 接口** - 设计并实现 HTTP/WS API
- [ ] **命令行工具** - 本地调用脚本

### Phase 2: 输入控制
- [ ] **鼠标控制** - 移动、点击、滚轮
- [ ] **键盘控制** - 按键、文本输入
- [ ] **组合键支持** - Ctrl+C, Alt+Tab 等

### Phase 3: 文件传输
- [ ] **SMB 优化** - 利用现有 SMB 连接
- [ ] **断点续传** - 大文件支持
- [ ] **目录同步** - 批量传输

### Phase 4: 高级功能
- [ ] **拖放操作** - 文件跨网络拖放
- [ ] **剪贴板同步** - 跨设备复制粘贴
- [ ] **多屏支持** - 多显示器环境

## 技术方案

### 通信协议

**方案 A: HTTP REST API**
- 简单易用
- 易于调试
- 缺点：延迟较高

**方案 B: WebSocket**
- 支持实时交互
- 双向通信
- 缺点：实现复杂

**方案 C: STDIO/命名管道**
- 适合本地调用
- 简单直接
- 缺点：需要辅助通道

**推荐方案**: HTTP API + WebSocket 混合
- 截图/文件用 HTTP
- 鼠标键盘用 WebSocket

### Agent 架构

```
Win7RCAgent/
├── AgentService.cs      # Windows 服务 (LocalSystem)
├── HttpServer.cs        # 内嵌 HTTP 服务器
├── SessionManager.cs    # 会话管理
├── Agents/
│   ├── ScreenshotAgent.cs   # 屏幕截图
│   ├── InputAgent.cs        # 鼠标/键盘
│   └── FileAgent.cs         # 文件传输
└── Protocols/
    ├── HttpHandler.cs
    └── WsHandler.cs
```

### API 设计

```
POST /api/screenshot          # 获取截图
POST /api/input/mouse         # 鼠标操作
POST /api/input/keyboard      # 键盘操作
POST /api/input/text          # 文本输入
POST /api/file/upload         # 上传文件
GET  /api/file/download       # 下载文件
GET  /ws/control             # WebSocket 控制通道
GET  /api/status              # 状态查询
```

### 请求格式

```json
// 截图
POST /api/screenshot
Response: { "image": "base64...", "width": 1440, "height": 900 }

// 鼠标移动
POST /api/input/mouse
Body: { "action": "move", "x": 100, "y": 200 }

// 鼠标点击
POST /api/input/mouse
Body: { "action": "click", "button": "left", "x": 100, "y": 200 }

// 键盘
POST /api/input/keyboard
Body: { "action": "key", "key": "A", "modifiers": ["ctrl"] }

// 文本输入
POST /api/input/text
Body: { "text": "hello world" }

// 文件上传
POST /api/file/upload
Body: multipart/form-data
```

## 本地客户端

### Python SDK

```python
class Win7Remote:
    def __init__(self, host, port=8080):
        self.base_url = f"http://{host}:{port}"

    def screenshot(self) -> Image:
        """获取屏幕截图"""

    def mouse_move(self, x, y):
        """移动鼠标"""

    def mouse_click(self, x, y, button='left'):
        """鼠标点击"""

    def keyboard_key(self, key, modifiers=None):
        """按键"""

    def keyboard_text(self, text):
        """文本输入"""

    def file_upload(self, local_path, remote_path):
        """上传文件"""

    def file_download(self, remote_path, local_path):
        """下载文件"""
```

## 开发任务

### Task 1: 项目重构
- [ ] 重构现有代码结构
- [ ] 添加 Agent 基础框架
- [ ] 实现 HTTP 服务器

### Task 2: API 实现
- [ ] 截图 API
- [ ] 鼠标控制 API
- [ ] 键盘控制 API
- [ ] 文件传输 API

### Task 3: 本地客户端
- [ ] Python SDK
- [ ] 命令行工具
- [ ] 交互式控制台

### Task 4: 优化
- [ ] 截图压缩优化
- [ ] 增量更新
- [ ] 错误处理

##里程碑

- [ ] M1: 基础框架运行 (1天)
- [ ] M2: 屏幕+鼠标键盘可用 (2天)
- [ ] M3: 文件传输完成 (1天)
- [ ] M4: 本地客户端完成 (1天)
- [ ] M5: 测试优化发布 (1天)

总计预计: 6天
