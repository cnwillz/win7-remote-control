#!/bin/bash
# Win7 Remote Control - 完整部署脚本
# 将本地修改同步到远程并重新部署

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
LOCAL_SRC="$PROJECT_DIR/src"
REMOTE_PUBLIC="C:\\Users\\Public"
LOCAL_SHARE="/tmp/win7share"

echo "=== Win7 Remote Control Deploy ==="

# 1. SMB 挂载
echo ""
echo "[1/5] Mount SMB share..."
if mount | grep -q "$LOCAL_SHARE"; then
    echo "  Already mounted"
else
    mkdir -p "$LOCAL_SHARE"
    mount_smbfs "//Administrator:123456@192.168.5.55/C$" "$LOCAL_SHARE"
    echo "  Mounted to $LOCAL_SHARE"
fi

# 2. 上传源文件
echo ""
echo "[2/5] Upload source files..."
cp "$LOCAL_SRC/HttpServer.cs" "$LOCAL_SHARE/Users/Public/HttpServer.cs"
echo "  Uploaded HttpServer.cs"
cp "$LOCAL_SRC/InputAgent.cs" "$LOCAL_SHARE/Users/Public/InputAgent.cs"
echo "  Uploaded InputAgent.cs"
if [ -f "$LOCAL_SRC/LauncherAgent.cs" ]; then
    cp "$LOCAL_SRC/LauncherAgent.cs" "$LOCAL_SHARE/Users/Public/LauncherAgent.cs"
    echo "  Uploaded LauncherAgent.cs"
fi

# 3. 远程执行部署
echo ""
echo "[3/5] Deploy via SSH..."
/usr/bin/python3 "$SCRIPT_DIR/ssh_remote.py" deploy --use-service

# 4. 测试 API
echo ""
echo "[4/5] Test APIs..."
/usr/bin/python3 "$SCRIPT_DIR/api_test.py" --test health

# 5. 检查服务状态
echo ""
echo "[5/5] Check service status..."
/usr/bin/python3 "$SCRIPT_DIR/ssh_remote.py" status --service Win7RCHttp

echo ""
echo "=== Deploy Complete ==="
