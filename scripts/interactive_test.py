#!/usr/bin/env python3
"""
Win7 Remote Control - 交互式测试脚本
正确流程: 截图 -> 分析位置 -> 执行操作 -> 截图验证

用法:
    ./scripts/interactive_test.py screenshot          # 获取截图
    ./scripts/interactive_test.py click <x> <y>      # 点击指定位置
    ./scripts/interactive_test.py move <x> <y>       # 移动鼠标
    ./scripts/interactive_test.py dblclick <x> <y>   # 双击
    ./scripts/interactive_test.py text <string>      # 输入文本
    ./scripts/interactive_test.py key <key>          # 按键
    ./scripts/interactive_test.py automate           # 自动化测试流程
"""
import requests
import sys
import os
import time
import base64
import argparse

HOST = "192.168.5.55"
PORT = 8080
BASE_URL = f"http://{HOST}:{PORT}"
SCREENSHOT_PATH = "/tmp/win7_screen.png"


def screenshot(filename=None):
    """获取截图并保存"""
    try:
        resp = requests.get(
            f"{BASE_URL}/api/screenshot",
            params={"format": "png", "quality": 80},
            timeout=30
        )
        if resp.status_code == 200:
            data = resp.json()
            img_data = base64.b64decode(data["image"])
            path = filename or SCREENSHOT_PATH
            with open(path, 'wb') as f:
                f.write(img_data)
            print(f"Screenshot saved: {path} ({len(img_data)} bytes)")
            print(f"Size: {data['width']}x{data['height']}")
            return path
        else:
            print(f"Error: {resp.status_code} - {resp.text}")
    except Exception as e:
        print(f"Error: {e}")
    return None


def mouse_move(x, y):
    """移动鼠标"""
    try:
        resp = requests.post(
            f"{BASE_URL}/api/input/mouse",
            json={"action": "move", "x": x, "y": y},
            timeout=10
        )
        result = resp.json()
        print(f"Move to ({x}, {y}): {result}")
        return result.get("success")
    except Exception as e:
        print(f"Error: {e}")
        return False


def mouse_click(x, y, button="left"):
    """点击"""
    try:
        resp = requests.post(
            f"{BASE_URL}/api/input/mouse",
            json={"action": "click", "x": x, "y": y, "button": button},
            timeout=10
        )
        result = resp.json()
        print(f"Click ({x}, {y}, {button}): {result}")
        return result.get("success")
    except Exception as e:
        print(f"Error: {e}")
        return False


def mouse_dblclick(x, y, button="left"):
    """双击"""
    # 两次快速点击
    mouse_click(x, y, button)
    time.sleep(0.2)
    mouse_click(x, y, button)


def mouse_drag(x1, y1, x2, y2, button="left"):
    """拖动"""
    try:
        resp = requests.post(
            f"{BASE_URL}/api/input/mouse",
            json={"action": "drag", "x1": x1, "y1": y1, "x2": x2, "y2": y2, "button": button},
            timeout=10
        )
        result = resp.json()
        print(f"Drag ({x1},{y1} -> {x2},{y2}): {result}")
        return result.get("success")
    except Exception as e:
        print(f"Error: {e}")
        return False


def keyboard_key(key, modifiers=""):
    """按键"""
    try:
        resp = requests.post(
            f"{BASE_URL}/api/input/keyboard",
            json={"action": "key", "key": key, "modifiers": modifiers},
            timeout=10
        )
        result = resp.json()
        print(f"Key '{key}' (modifiers={modifiers}): {result}")
        return result.get("success")
    except Exception as e:
        print(f"Error: {e}")
        return False


def keyboard_text(text):
    """文本输入"""
    try:
        resp = requests.post(
            f"{BASE_URL}/api/input/text",
            json={"text": text},
            timeout=10
        )
        result = resp.json()
        print(f"Text '{text}': {result}")
        return result.get("success")
    except Exception as e:
        print(f"Error: {e}")
        return False


def wait_for_open():
    """等待用户查看"""
    input("按回车继续...")


def automate():
    """自动化测试流程"""
    print("=" * 60)
    print("Win7 Remote Control - 自动化测试流程")
    print("=" * 60)

    # Step 1: 获取桌面截图
    print("\n[Step 1] 获取桌面截图...")
    path = screenshot()
    if not path:
        print("截图失败!")
        return
    print(f"截图已保存到: {path}")
    wait_for_open()

    # Step 2: 移动鼠标到屏幕中央
    print("\n[Step 2] 移动鼠标到屏幕中央 (720, 450)...")
    mouse_move(720, 450)
    wait_for_open()

    # Step 3: 双击打开回收站
    print("\n[Step 3] 双击打开回收站 (假设在 1350, 280)...")
    print("提示: 请根据实际截图确定回收站位置")
    # 先移动到目标
    mouse_move(1350, 280)
    wait_for_open()
    # 双击
    print("执行双击...")
    mouse_dblclick(1350, 280)
    wait_for_open()

    # Step 4: 关闭弹出的窗口
    print("\n[Step 4] 关闭弹出的窗口 (Alt+F4)...")
    keyboard_key("F4", "alt")
    wait_for_open()

    # Step 5: 测试记事本
    print("\n[Step 5] 打开记事本 (Win+R -> notepad -> Enter)...")
    keyboard_key("0x5B")  # Win 键
    time.sleep(0.3)
    keyboard_text("notepad")
    time.sleep(0.3)
    keyboard_key("enter")
    wait_for_open()

    # Step 6: 输入文本
    print("\n[Step 6] 在记事本中输入文本...")
    keyboard_text("Win7 Remote Control Test")
    wait_for_open()

    # Step 7: 关闭记事本
    print("\n[Step 7] 关闭记事本 (Alt+F4)...")
    keyboard_key("F4", "alt")
    wait_for_open()

    print("\n" + "=" * 60)
    print("测试流程完成!")
    print("=" * 60)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Win7 Remote Control 交互式测试")
    parser.add_argument('action', choices=['screenshot', 'click', 'move', 'dblclick', 'drag', 'text', 'key', 'automate'],
                        help='操作类型')
    parser.add_argument('params', nargs='*', help='参数: x y 或 text')

    args = parser.parse_args()

    if args.action == 'screenshot':
        screenshot()

    elif args.action == 'click':
        if len(args.params) < 2:
            print("用法: click <x> <y>")
        else:
            mouse_click(int(args.params[0]), int(args.params[1]))

    elif args.action == 'move':
        if len(args.params) < 2:
            print("用法: move <x> <y>")
        else:
            mouse_move(int(args.params[0]), int(args.params[1]))

    elif args.action == 'dblclick':
        if len(args.params) < 2:
            print("用法: dblclick <x> <y>")
        else:
            mouse_dblclick(int(args.params[0]), int(args.params[1]))

    elif args.action == 'drag':
        if len(args.params) < 4:
            print("用法: drag <x1> <y1> <x2> <y2>")
        else:
            mouse_drag(int(args.params[0]), int(args.params[1]),
                      int(args.params[2]), int(args.params[3]))

    elif args.action == 'text':
        if not args.params:
            print("用法: text <string>")
        else:
            keyboard_text(' '.join(args.params))

    elif args.action == 'key':
        if not args.params:
            print("用法: key <keyname>")
        else:
            keyboard_key(args.params[0])

    elif args.action == 'automate':
        automate()
