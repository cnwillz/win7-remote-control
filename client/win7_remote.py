#!/usr/bin/env python3
"""
Win7 Remote Control Client SDK
"""
import requests
import base64
import io
from typing import Optional, Tuple
from PIL import Image


class Win7Remote:
    """Windows 7 远程控制客户端"""

    def __init__(self, host: str = "192.168.5.55", port: int = 8080):
        self.host = host
        self.port = port
        self.base_url = f"http://{host}:{port}"
        self.session = requests.Session()

    def screenshot(self) -> Optional[Image.Image]:
        """获取屏幕截图"""
        try:
            resp = self.session.get(f"{self.base_url}/api/screenshot", timeout=10)
            if resp.status_code == 200:
                data = resp.json()
                img_data = base64.b64decode(data["image"])
                return Image.open(io.BytesIO(img_data))
        except Exception as e:
            print(f"Screenshot error: {e}")
        return None

    def mouse_move(self, x: int, y: int) -> bool:
        """移动鼠标"""
        try:
            resp = self.session.post(
                f"{self.base_url}/api/input/mouse",
                json={"action": "move", "x": x, "y": y},
                timeout=5
            )
            return resp.status_code == 200
        except Exception as e:
            print(f"Mouse move error: {e}")
        return False

    def mouse_click(self, x: int, y: int, button: str = "left") -> bool:
        """鼠标点击"""
        try:
            resp = self.session.post(
                f"{self.base_url}/api/input/mouse",
                json={"action": "click", "button": button, "x": x, "y": y},
                timeout=5
            )
            return resp.status_code == 200
        except Exception as e:
            print(f"Mouse click error: {e}")
        return False

    def mouse_drag(self, x1: int, y1: int, x2: int, y2: int, button: str = "left") -> bool:
        """鼠标拖动"""
        try:
            resp = self.session.post(
                f"{self.base_url}/api/input/mouse",
                json={"action": "drag", "button": button, "x1": x1, "y1": y1, "x2": x2, "y2": y2},
                timeout=5
            )
            return resp.status_code == 200
        except Exception as e:
            print(f"Mouse drag error: {e}")
        return False

    def keyboard_key(self, key: str, modifiers: Optional[list] = None) -> bool:
        """键盘按键"""
        try:
            resp = self.session.post(
                f"{self.base_url}/api/input/keyboard",
                json={"action": "key", "key": key, "modifiers": modifiers or []},
                timeout=5
            )
            return resp.status_code == 200
        except Exception as e:
            print(f"Keyboard error: {e}")
        return False

    def keyboard_text(self, text: str) -> bool:
        """文本输入"""
        try:
            resp = self.session.post(
                f"{self.base_url}/api/input/text",
                json={"text": text},
                timeout=5
            )
            return resp.status_code == 200
        except Exception as e:
            print(f"Keyboard text error: {e}")
        return False

    def file_upload(self, local_path: str, remote_path: str) -> bool:
        """上传文件"""
        try:
            with open(local_path, 'rb') as f:
                files = {'file': f}
                data = {'path': remote_path}
                resp = self.session.post(
                    f"{self.base_url}/api/file/upload",
                    files=files,
                    data=data,
                    timeout=60
                )
            return resp.status_code == 200
        except Exception as e:
            print(f"File upload error: {e}")
        return False

    def file_download(self, remote_path: str, local_path: str) -> bool:
        """下载文件"""
        try:
            resp = self.session.get(
                f"{self.base_url}/api/file/download",
                params={"path": remote_path},
                timeout=60
            )
            if resp.status_code == 200:
                with open(local_path, 'wb') as f:
                    f.write(resp.content)
                return True
        except Exception as e:
            print(f"File download error: {e}")
        return False

    def status(self) -> dict:
        """获取状态"""
        try:
            resp = self.session.get(f"{self.base_url}/api/status", timeout=5)
            if resp.status_code == 200:
                return resp.json()
        except Exception as e:
            print(f"Status error: {e}")
        return {}


if __name__ == "__main__":
    # 测试
    client = Win7Remote()

    # 获取截图
    img = client.screenshot()
    if img:
        img.save("/tmp/test_screenshot.png")
        print("Screenshot saved")

    # 移动鼠标
    client.mouse_move(100, 100)
    client.mouse_click(100, 100)
