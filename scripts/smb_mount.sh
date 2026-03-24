#!/bin/bash
# SMB 挂载 Win7 远程目录
# 用法: ./scripts/smb_mount.sh [mount|umount]

ACTION=${1:-mount}
REMOTE_HOST="192.168.5.55"
LOCAL_MOUNT="/tmp/win7share"
CREDENTIALS="Administrator:123456"

if [ "$ACTION" = "mount" ]; then
    if mount | grep -q "$LOCAL_MOUNT"; then
        echo "Already mounted at $LOCAL_MOUNT"
    else
        mkdir -p "$LOCAL_MOUNT"
        mount_smbfs "//$CREDENTIALS@$REMOTE_HOST/C$" "$LOCAL_MOUNT"
        echo "Mounted to $LOCAL_MOUNT"
    fi
elif [ "$ACTION" = "umount" ]; then
    umount "$LOCAL_MOUNT" 2>/dev/null && echo "Unmounted" || echo "Not mounted"
else
    echo "Usage: $0 [mount|umount]"
fi
