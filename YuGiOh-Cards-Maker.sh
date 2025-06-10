#!/bin/zsh
# YuGiOh-Cards-Maker.sh - 环境检查和项目启动脚本
# 确保脚本在错误时退出
set -e
# 输出彩色文本的函数
print_green() {
    echo -e "\033[0;32m$1\033[0m"
}

print_yellow() {
    echo -e "\033[0;33m$1\033[0m"
}

print_red() {
    echo -e "\033[0;31m$1\033[0m"
}
# 检查命令是否存在的函数
check_command() {
    if ! command -v $1 &> /dev/null; then
        print_yellow "未找到命令: $1，尝试安装..."
        return 1
    else
        return 0
    fi
}
# 检查和安装依赖
install_dependencies() {
    print_green "=== 检查系统依赖 ==="
    # 确保包管理器是最新的
    if command -v apt &> /dev/null; then
        print_yellow "使用 apt 包管理器..."
        sudo apt update || print_red "无法更新软件包列表，但将继续尝试..."
        INSTALL_CMD="sudo apt install -y"
    elif command -v dnf &> /dev/null; then
        print_yellow "使用 dnf 包管理器..."
        INSTALL_CMD="sudo dnf install -y"
    elif command -v yum &> /dev/null; then
        print_yellow "使用 yum 包管理器..."
        INSTALL_CMD="sudo yum install -y"
    elif command -v pacman &> /dev/null; then
        print_yellow "使用 pacman 包管理器..."
        INSTALL_CMD="sudo pacman -S --noconfirm"
    elif command -v zypper &> /dev/null; then
        print_yellow "使用 zypper 包管理器..."
        INSTALL_CMD="sudo zypper install -y"
    else
        print_red "无法识别的包管理器。请手动安装以下依赖: wget curl unzip jq imagemagick dotnet-sdk-8.0"
        print_yellow "按任意键继续尝试运行项目，或按Ctrl+C退出..."
        read -k1
        return
    fi
    # 检查并安装 wget
    if ! check_command wget; then
        $INSTALL_CMD wget || print_red "无法安装 wget，某些功能可能不可用"
    fi
    # 检查并安装 curl
    if ! check_command curl; then
        $INSTALL_CMD curl || print_red "无法安装 curl，某些功能可能不可用"
    fi
    # 检查并安装 unzip
    if ! check_command unzip; then
        $INSTALL_CMD unzip || print_red "无法安装 unzip，某些功能可能不可用"
    fi
    # 检查并安装 jq
    if ! check_command jq; then
        $INSTALL_CMD jq || print_red "无法安装 jq，某些功能可能不可用"
    fi
    # 检查并安装 ImageMagick
    if ! check_command convert || ! check_command identify; then
        $INSTALL_CMD imagemagick || print_red "无法安装 ImageMagick，某些功能可能不可用"
    fi
    # 检查 .NET SDK
    if ! check_command dotnet; then
        print_yellow "未找到 .NET SDK，尝试安装 .NET 8.0..."
        # 尝试为不同的发行版安装 .NET SDK
        if command -v apt &> /dev/null; then
            # Debian/Ubuntu
            wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
            sudo dpkg -i packages-microsoft-prod.deb
            rm packages-microsoft-prod.deb
            sudo apt update
            sudo apt install -y dotnet-sdk-8.0
        elif command -v dnf &> /dev/null || command -v yum &> /dev/null; then
            # RHEL/Fedora
            sudo rpm -Uvh https://packages.microsoft.com/config/rhel/8/packages-microsoft-prod.rpm
            if command -v dnf &> /dev/null; then
                sudo dnf install -y dotnet-sdk-8.0
            else
                sudo yum install -y dotnet-sdk-8.0
            fi
        elif command -v pacman &> /dev/null; then
            # Arch Linux
            sudo pacman -S --noconfirm dotnet-sdk
        elif command -v zypper &> /dev/null; then
            # OpenSUSE
            sudo zypper install -y dotnet-sdk-8.0
        else
            print_red "无法自动安装 .NET SDK。请访问 https://dotnet.microsoft.com/download/dotnet/8.0 手动安装"
            print_yellow "按任意键继续尝试运行项目，或按Ctrl+C退出..."
            read -k1
        fi
    fi
    # 验证 .NET 版本
    if command -v dotnet &> /dev/null; then
        DOTNET_VERSION=$(dotnet --version)
        print_green "已安装的 .NET 版本: $DOTNET_VERSION"
        if [[ "$DOTNET_VERSION" != 8.* ]]; then
            print_yellow "警告: 项目需要 .NET 8.0，但当前版本是 $DOTNET_VERSION"
            print_yellow "建议访问 https://dotnet.microsoft.com/download/dotnet/8.0 安装 .NET 8.0"
            print_yellow "按任意键继续尝试运行项目，或按Ctrl+C退出..."
            read -k1
        fi
    else
        print_red "无法检测到 .NET SDK，项目可能无法运行"
        print_yellow "按任意键继续尝试运行项目，或按Ctrl+C退出..."
        read -k1
    fi
}
# 设置系统限制
setup_system_limits() {
    print_green "=== 设置系统限制 ==="
    current_fd_limit=$(ulimit -n)
    if [ "$current_fd_limit" -lt 4096 ]; then
        print_yellow "增加文件描述符限制到4096（原限制：$current_fd_limit）"
        ulimit -n 4096 || print_yellow "警告: 无法增加文件描述符限制，可能需要root权限"
    fi
    current_proc_limit=$(ulimit -u)
    if [ "$current_proc_limit" -lt 4096 ]; then
        print_yellow "增加进程数限制到4096（原限制：$current_proc_limit）"
        ulimit -u 4096 || print_yellow "警告: 无法增加进程数限制，可能需要root权限"
    fi
}
# 创建必要的目录
create_directories() {
    print_green "=== 创建必要目录 ==="
    
    mkdir -p tmp
    mkdir -p figure
    mkdir -p tmp/figure
}
# 运行主程序
run_project() {
    print_green "=== 运行 YuGiOh Cards Maker ==="
    # 运行数据处理脚本
    print_yellow "正在运行卡片数据处理脚本..."
    if [ -f "script/process_yugioh_cards.sh" ]; then
        chmod +x script/process_yugioh_cards.sh
        ./script/process_yugioh_cards.sh
        if [ $? -ne 0 ]; then
            print_red "卡片数据处理脚本失败"
            exit 1
        fi
    else
        print_red "找不到数据处理脚本: script/process_yugioh_cards.sh"
        exit 1
    fi
    # 运行 .NET 项目
    print_yellow "正在编译和运行 .NET 项目..."
    dotnet run
}
# 主程序
main() {
    print_green "===== YuGiOh Cards Maker 启动脚本 ====="
    # 切换到脚本所在目录
    SCRIPT_DIR=$(dirname "$0")
    cd "$SCRIPT_DIR" || exit 1
    # 确保我们在项目根目录
    if [ ! -f "Program.cs" ] || [ ! -f "YuGiOh-Cards-Maker.csproj" ]; then
        print_red "错误: 脚本必须在项目根目录运行"
        exit 1
    fi
    # 检查和安装依赖
    install_dependencies
    # 设置系统限制
    setup_system_limits
    # 创建必要的目录
    create_directories
    # 运行项目
    run_project
    print_green "===== YuGiOh Cards Maker 执行完成 ====="
}
# 启动主程序
main "$@"