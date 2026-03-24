#!/usr/bin/env python3
"""
Win7 Remote Control - API 测试脚本
测试 HttpServer 的各个 API 端点
"""
import requests
import sys
import argparse
import json

HOST = "192.168.5.55"
PORT = 8080
BASE_URL = f"http://{HOST}:{PORT}"


def test_screenshot(format="jpeg", quality=70, scale=1.0):
    """测试截图 API"""
    print(f"\n=== Screenshot (format={format}, quality={quality}, scale={scale}) ===")
    try:
        resp = requests.get(
            f"{BASE_URL}/api/screenshot",
            params={"format": format, "quality": quality, "scale": scale},
            timeout=30
        )
        print(f"Status: {resp.status_code}")
        if resp.status_code == 200:
            data = resp.json()
            print(f"Image size: {data.get('size', 0)} bytes")
            print(f"Dimensions: {data.get('width')}x{data.get('height')}")
            print(f"Format: {data.get('format')}")
            return True
        else:
            print(f"Error: {resp.text}")
    except Exception as e:
        print(f"Error: {e}")
    return False


def test_mouse_move(x, y):
    """测试鼠标移动"""
    print(f"\n=== Mouse Move ({x}, {y}) ===")
    try:
        resp = requests.post(
            f"{BASE_URL}/api/input/mouse",
            json={"action": "move", "x": x, "y": y},
            timeout=10
        )
        print(f"Status: {resp.status_code}, Body: {resp.text}")
        return resp.status_code == 200 and resp.json().get("success")
    except Exception as e:
        print(f"Error: {e}")
        return False


def test_mouse_click(x, y, button="left"):
    """测试鼠标点击"""
    print(f"\n=== Mouse Click ({x}, {y}, button={button}) ===")
    try:
        resp = requests.post(
            f"{BASE_URL}/api/input/mouse",
            json={"action": "click", "x": x, "y": y, "button": button},
            timeout=10
        )
        print(f"Status: {resp.status_code}, Body: {resp.text}")
        return resp.status_code == 200 and resp.json().get("success")
    except Exception as e:
        print(f"Error: {e}")
        return False


def test_mouse_drag(x1, y1, x2, y2, button="left"):
    """测试鼠标拖动"""
    print(f"\n=== Mouse Drag ({x1},{y1} -> {x2},{y2}, button={button}) ===")
    try:
        resp = requests.post(
            f"{BASE_URL}/api/input/mouse",
            json={"action": "drag", "x1": x1, "y1": y1, "x2": x2, "y2": y2, "button": button},
            timeout=10
        )
        print(f"Status: {resp.status_code}, Body: {resp.text}")
        return resp.status_code == 200 and resp.json().get("success")
    except Exception as e:
        print(f"Error: {e}")
        return False


def test_keyboard(key, modifiers=""):
    """测试键盘按键"""
    print(f"\n=== Keyboard ({key}, modifiers={modifiers}) ===")
    try:
        resp = requests.post(
            f"{BASE_URL}/api/input/keyboard",
            json={"action": "key", "key": key, "modifiers": modifiers},
            timeout=10
        )
        print(f"Status: {resp.status_code}, Body: {resp.text}")
        return resp.status_code == 200 and resp.json().get("success")
    except Exception as e:
        print(f"Error: {e}")
        return False


def test_text(text):
    """测试文本输入"""
    print(f"\n=== Text Input ('{text}') ===")
    try:
        resp = requests.post(
            f"{BASE_URL}/api/input/text",
            json={"text": text},
            timeout=10
        )
        print(f"Status: {resp.status_code}, Body: {resp.text}")
        return resp.status_code == 200 and resp.json().get("success")
    except Exception as e:
        print(f"Error: {e}")
        return False


def test_status():
    """测试状态 API"""
    print("\n=== Status ===")
    try:
        resp = requests.get(f"{BASE_URL}/api/status", timeout=5)
        print(f"Status: {resp.status_code}, Body: {resp.text}")
        return resp.status_code == 200
    except Exception as e:
        print(f"Error: {e}")
        return False


def test_health():
    """健康检查"""
    print("\n=== Health Check ===")
    try:
        resp = requests.get(f"{BASE_URL}/health", timeout=5)
        print(f"Status: {resp.status_code}, Body: {resp.text}")
        return resp.status_code == 200
    except Exception as e:
        print(f"Error: {e}")
        return False


def test_all():
    """完整测试"""
    print("=" * 50)
    print("Win7 Remote Control API Test")
    print("=" * 50)

    results = []

    results.append(("Health", test_health()))
    results.append(("Status", test_status()))
    results.append(("Screenshot PNG", test_screenshot("png")))
    results.append(("Screenshot JPEG 70%", test_screenshot("jpeg", 70)))
    results.append(("Screenshot JPEG 50%", test_screenshot("jpeg", 50)))
    results.append(("Screenshot Scale 0.5", test_screenshot("jpeg", 70, 0.5)))
    results.append(("Mouse Move", test_mouse_move(100, 100)))
    results.append(("Mouse Click", test_mouse_click(500, 300)))
    results.append(("Mouse Drag", test_mouse_drag(100, 100, 300, 300)))
    results.append(("Keyboard A", test_keyboard("a")))
    results.append(("Keyboard Enter", test_keyboard("enter")))
    results.append(("Text Hello", test_text("hello")))

    print("\n" + "=" * 50)
    print("Summary")
    print("=" * 50)
    for name, result in results:
        status = "PASS" if result else "FAIL"
        print(f"  {name:20s}: {status}")

    passed = sum(1 for _, r in results if r)
    print(f"\nTotal: {passed}/{len(results)} passed")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Win7 Remote API Test")
    parser.add_argument('--host', default=HOST)
    parser.add_argument('--port', type=int, default=PORT)
    parser.add_argument('--test', choices=['all', 'input', 'screenshot', 'health'],
                       default='all')
    parser.add_argument('--x', type=int, default=500)
    parser.add_argument('--y', type=int, default=300)

    args = parser.parse_args()

    if args.test == 'all':
        test_all()
    elif args.test == 'input':
        test_mouse_move(args.x, args.y)
        test_mouse_click(args.x, args.y)
        test_keyboard("a")
        test_text("hello")
    elif args.test == 'screenshot':
        test_screenshot()
    elif args.test == 'health':
        test_health()
        test_status()
