# Win7 远程控制脚本

## 快速开始

### 1. 挂载 SMB 共享
```bash
./scripts/smb_mount.sh mount
# 挂载到 /tmp/win7share
```

### 2. 部署到远程
```bash
./scripts/deploy.sh
```
会自动: 上传源文件 → 远程编译 → 重启服务 → 测试 API

### 3. 手动控制

#### 远程执行命令
```bash
# 部署 (编译 + 重启)
./scripts/ssh_remote.py deploy
./scripts/ssh_remote.py deploy --use-service  # 通过服务启动 (用户 Session)

# 杀进程
./scripts/ssh_remote.py kill

# 查看状态
./scripts/ssh_remote.py status
./scripts/ssh_remote.py status --service Win7RCHttp

# 服务控制
./scripts/ssh_remote.py service-start
./scripts/ssh_remote.py service-stop

# 查看日志
./scripts/ssh_remote.py log --log-lines 30
```

#### API 测试
```bash
# 完整测试
./scripts/api_test.py --test all

# 只测输入
./scripts/api_test.py --test input --x 500 --y 300

# 只测截图
./scripts/api_test.py --test screenshot

# 健康检查
./scripts/api_test.py --test health
```

## 脚本说明

| 脚本 | 说明 |
|------|------|
| `smb_mount.sh` | SMB 挂载/卸载 Win7 共享目录 |
| `ssh_remote.py` | 远程执行命令,编译,部署 |
| `api_test.py` | 测试 HttpServer API (快速验证) |
| `interactive_test.py` | 交互式测试 (截图→分析→操作→验证) |
| `deploy.sh` | 完整部署流程 |

## 连接信息

- **IP**: 192.168.5.55
- **SMB**: `mount_smbfs //Administrator:123456@192.168.5.55/C$ /tmp/win7share`
- **SSH**: Administrator / 123456 (用 `/usr/bin/python3` + paramiko)
- **HttpServer**: http://192.168.5.55:8080

## API 端点

| 端点 | 方法 | 说明 |
|------|------|------|
| `/api/screenshot` | GET | 获取屏幕截图 (format, quality, scale 参数) |
| `/api/status` | GET | 获取状态 |
| `/api/input/mouse` | POST | 鼠标操作 (move/click/drag/wheel) |
| `/api/input/keyboard` | POST | 键盘按键 |
| `/api/input/text` | POST | 文本输入 |
| `/api/file/upload` | POST | 文件上传 (Base64) |
| `/api/file/download` | GET | 文件下载 |
| `/health` | GET | 健康检查 |

## Session 0 问题

HttpServer 通过 SSH 直接启动运行在 Session 0 (Services),无法捕获用户桌面。
需要通过 `Win7RCHttp` 服务启动才能在用户 Session 1 运行。

```bash
# 查看服务状态
sc query Win7RCHttp

# 启动服务 (服务会自动启动 HttpServer.exe)
sc start Win7RCHttp
```

## 重要测试发现

⚠️ **GDI BitBlt 截图不显示鼠标光标**,无法从截图确认鼠标位置

**已验证可用的坐标**:
- 任务栏开始菜单按钮: (720, 870) - 点击成功打开开始菜单
- 桌面图标在屏幕**左侧**第一列

**正确测试流程**:
1. 截图
2. 仔细分析截图确定目标位置
3. 执行操作
4. 截图验证结果

## 已知问题

- Session 0 隔离: 直接运行的 HttpServer 无法截取用户桌面
- 文件锁定: 编译时如果 HttpServer 正在运行会报 "另一个程序正在使用此文件"
  - 解决: 先 `ssh_remote.py kill` 再编译
