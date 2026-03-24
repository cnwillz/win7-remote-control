#!/usr/bin/env python3
"""
Win7 远程控制 - SSH/Paramiko 远程执行脚本
使用系统 Python (/usr/bin/python3) 避免与 Homebrew Python 冲突

连接参数:
- Host: 192.168.5.55
- User: Administrator
- Pass: 123456
"""
import paramiko
import sys
import time
import argparse

HOST = "192.168.5.55"
USER = "Administrator"
PASS = "123456"
REMOTE_PUBLIC = r"C:\Users\Public"
LOCAL_SHARE = "/tmp/win7share"


def ssh_connect():
    ssh = paramiko.SSHClient()
    ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    ssh.connect(HOST, username=USER, password=PASS, timeout=15)
    return ssh


def cmd(ssh, command, encoding='gbk'):
    """执行远程命令并返回输出"""
    stdin, stdout, stderr = ssh.exec_command(command)
    out = stdout.read().decode(encoding, errors='replace').strip()
    err = stderr.read().decode(encoding, errors='replace').strip()
    return out, err


def kill_httpServer(ssh):
    """停止所有 HttpServer 进程"""
    print("=== Kill HttpServer ===")
    for _ in range(3):
        cmd(ssh, 'taskkill /F /IM HttpServer.exe 2>nul')
        time.sleep(0.5)
    time.sleep(1)
    out, _ = cmd(ssh, 'tasklist /FI "IMAGENAME eq HttpServer.exe"')
    print(out)


def kill_all(ssh):
    """停止所有相关进程"""
    print("=== Kill all processes ===")
    cmd(ssh, 'taskkill /F /IM HttpServer.exe 2>nul')
    cmd(ssh, 'taskkill /F /IM LauncherAgent.exe 2>nul')
    time.sleep(1)
    out, _ = cmd(ssh, 'tasklist | findstr HttpServer')
    print(f"Remaining HttpServer: '{out}'")


def compile_cs(ssh, cs_file, out_file, extra_refs=""):
    """编译 C# 源文件"""
    compiler = r"C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
    cmd_line = f'{compiler} /target:exe /out:{out_file} {extra_refs} {cs_file}'
    print(f"=== Compile {cs_file} ===")
    out, err = cmd(ssh, cmd_line)
    print(out)
    if err:
        print("STDERR:", err)
    return 'error' not in out.lower()


def start_httpServer(ssh, port=8080):
    """通过 SSH 启动 HttpServer (Session 0)"""
    print(f"=== Start HttpServer on port {port} ===")
    ssh.exec_command(rf'cd {REMOTE_PUBLIC} && HttpServer.exe {port}')
    time.sleep(2)
    out, _ = cmd(ssh, 'tasklist /FI "IMAGENAME eq HttpServer.exe"')
    print(out)


def start_httpServer_via_service(ssh):
    """通过 Windows 服务启动 HttpServer (用户 Session)"""
    print("=== Start via Win7RCHttp service ===")
    cmd(ssh, 'sc start Win7RCHttp')
    time.sleep(5)
    out, _ = cmd(ssh, 'tasklist /FI "IMAGENAME eq HttpServer.exe"')
    print(out)


def stop_service(ssh, service_name):
    """停止 Windows 服务"""
    print(f"=== Stop {service_name} ===")
    cmd(ssh, f'sc stop {service_name}')
    time.sleep(2)


def service_status(ssh, service_name):
    """查询服务状态"""
    out, _ = cmd(ssh, f'sc query {service_name}')
    return out


def deploy_and_restart(ssh, cs_file="HttpServer.cs", exe_file="HttpServer.exe",
                       new_name=None, use_service=False):
    """
    部署流程: 杀进程 -> 编译 -> 启动

    Args:
        ssh: paramiko SSH 连接
        cs_file: 源文件名 (在 C:\\Users\\Public\\ 下)
        exe_file: 输出 exe 名
        new_name: 临时编译文件名 (用于避免文件锁定)
        use_service: 是否通过 LauncherService 启动 (用户 Session)
    """
    if new_name:
        temp_exe = f"{REMOTE_PUBLIC}\\{new_name}"
        final_exe = f"{REMOTE_PUBLIC}\\{exe_file}"
    else:
        temp_exe = f"{REMOTE_PUBLIC}\\{exe_file}"
        final_exe = None

    # 1. 停止进程
    if use_service:
        # 通过服务启动时,先停服务
        stop_service(ssh, 'LauncherService')
    kill_all(ssh)

    # 2. 编译到临时文件
    success = compile_cs(ssh, f"{REMOTE_PUBLIC}\\{cs_file}", temp_exe)
    if not success:
        print("Compile failed!")
        return False

    # 3. 替换旧文件
    if final_exe and final_exe != temp_exe:
        print(f"=== Replace {exe_file} ===")
        cmd(ssh, rf'copy /Y {temp_exe} {final_exe}')

    # 4. 启动
    if use_service:
        start_httpServer_via_service(ssh)
    else:
        start_httpServer(ssh)

    return True


def install_service(ssh, exe_path, service_name, display_name):
    """安装 Windows 服务"""
    print(f"=== Install service {service_name} ===")
    cmd(ssh, rf'sc create {service_name} binPath= "{exe_path}"')
    cmd(ssh, rf'sc config {service_name} DisplayName= "{display_name}"')
    cmd(ssh, rf'sc config {service_name} type= interact type= own')
    out, _ = cmd(ssh, f'sc query {service_name}')
    print(out)


def check_service_exists(ssh, service_name):
    """检查服务是否存在"""
    out, _ = cmd(ssh, f'sc query {service_name}')
    return 'SERVICE_NAME' in out or '1060' not in out


def get_log(ssh, log_file, lines=20, encoding='gbk'):
    """获取远程日志文件"""
    cmd_line = f'powershell -Command "Get-Content {log_file} -Tail {lines}"'
    out, _ = cmd(ssh, cmd_line, encoding='utf-8')
    return out


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Win7 远程控制脚本")
    parser.add_argument('action', choices=['deploy', 'kill', 'status', 'service-start', 'service-stop', 'log'],
                        help='操作类型')
    parser.add_argument('--cs', default='HttpServer.cs', help='C# 源文件')
    parser.add_argument('--exe', default='HttpServer.exe', help='输出 exe')
    parser.add_argument('--port', type=int, default=8080, help='HttpServer 端口')
    parser.add_argument('--service', default='Win7RCHttp', help='服务名')
    parser.add_argument('--log-lines', type=int, default=20, help='日志行数')
    parser.add_argument('--use-service', action='store_true', help='通过服务启动')

    args = parser.parse_args()

    ssh = ssh_connect()

    try:
        if args.action == 'deploy':
            deploy_and_restart(ssh, args.cs, args.exe, use_service=args.use_service)
        elif args.action == 'kill':
            kill_all(ssh)
        elif args.action == 'status':
            out, _ = cmd(ssh, 'tasklist /FI "IMAGENAME eq HttpServer.exe"')
            print(out)
            if args.service:
                out = service_status(ssh, args.service)
                print(out)
        elif args.action == 'service-start':
            out, _ = cmd(ssh, f'sc start {args.service}')
            print(out)
            time.sleep(5)
            out, _ = cmd(ssh, 'tasklist /FI "IMAGENAME eq HttpServer.exe"')
            print(out)
        elif args.action == 'service-stop':
            out, _ = cmd(ssh, f'sc stop {args.service}')
            print(out)
            time.sleep(2)
            kill_all(ssh)
        elif args.action == 'log':
            log_path = f"{REMOTE_PUBLIC}\\LauncherService.log"
            print(get_log(ssh, log_path, args.log_lines))
    finally:
        ssh.close()
