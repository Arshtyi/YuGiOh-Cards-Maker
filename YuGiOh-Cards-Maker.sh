#!/bin/bash
set -e
print_green() {
    echo -e "\033[0;32m$1\033[0m"
}
print_yellow() {
    echo -e "\033[0;33m$1\033[0m"
}
print_red() {
    echo -e "\033[0;31m$1\033[0m"
}
check_command() {
    if ! command -v $1 &> /dev/null; then
        print_yellow "未找到命令: $1,尝试安装..."
        return 1
    else
        return 0
    fi
}
install_dependencies() {
    if [ -n "$GITHUB_ACTIONS" ]; then
        print_green "=== 在GitHub Actions环境中运行,跳过依赖安装 ==="
        return
    fi
    print_green "=== 检查系统依赖 ==="
    print_yellow "使用 dnf 包管理器..."
    INSTALL_CMD="sudo dnf install -y"
    if ! check_command wget; then
        $INSTALL_CMD wget || print_red "无法安装 wget,某些功能可能不可用"
    fi
    if ! check_command curl; then
        $INSTALL_CMD curl || print_red "无法安装 curl,某些功能可能不可用"
    fi
    if ! check_command unzip; then
        $INSTALL_CMD unzip || print_red "无法安装 unzip,某些功能可能不可用"
    fi
    if ! check_command jq; then
        $INSTALL_CMD jq || print_red "无法安装 jq,某些功能可能不可用"
    fi
    if ! check_command magick; then
        $INSTALL_CMD imagemagick || print_red "无法安装 ImageMagick,某些功能可能不可用"
    fi
    if ! check_command pip && ! check_command pip3; then
        $INSTALL_CMD python3-pip || print_red "无法安装 pip,某些功能可能不可用"
    fi
    if [ -f "requirements.txt" ]; then
        print_yellow "安装requirements.txt中的Python依赖..."
        if command -v pip &> /dev/null; then
            print_yellow "使用pip安装Python依赖..."
            pip install -r requirements.txt || print_red "pip安装Python依赖失败,尝试其他方法..."
        elif command -v pip3 &> /dev/null; then
            print_yellow "使用pip3安装Python依赖..."
            pip3 install -r requirements.txt || print_red "pip3安装Python依赖失败,尝试其他方法..."
        elif command -v python &> /dev/null; then
            print_yellow "使用python -m pip安装Python依赖..."
            python -m pip install -r requirements.txt || print_red "python -m pip安装Python依赖失败,尝试其他方法..."
        elif command -v python3 &> /dev/null; then
            print_yellow "使用python3 -m pip安装Python依赖..."
            python3 -m pip install -r requirements.txt || print_red "python3 -m pip安装Python依赖失败,某些功能可能不可用"
        else
            print_red "无法找到任何可用的pip命令安装Python依赖,某些功能可能不可用"
        fi
    else
        print_red "找不到requirements.txt文件,跳过Python依赖安装"
    fi
    if ! check_command dotnet; then
        print_yellow "未找到 .NET SDK,尝试安装 .NET 8.0..."
        sudo rpm -Uvh https://packages.microsoft.com/config/rhel/8/packages-microsoft-prod.rpm
        sudo dnf install -y dotnet-sdk-8.0
    fi
    if command -v dotnet &> /dev/null; then
        DOTNET_VERSION=$(dotnet --version)
        print_green "已安装的 .NET 版本: $DOTNET_VERSION"
        if [[ "$DOTNET_VERSION" != 8.* ]]; then
            print_yellow "警告: 项目需要 .NET 8.0,但当前版本是 $DOTNET_VERSION"        print_yellow "建议访问 https://dotnet.microsoft.com/download/dotnet/8.0 安装 .NET 8.0"
        print_yellow "按任意键继续尝试运行项目,或按Ctrl+C退出..."
        read -n1
        fi
    else
        print_red "无法检测到 .NET SDK,项目可能无法运行"
        print_yellow "按任意键继续尝试运行项目,或按Ctrl+C退出..."
        read -n1
    fi
}
create_directories() {
    print_green "=== 创建必要目录 ==="
    mkdir -p tmp/figure
}
cleanup_directories() {
    print_green "=== 清理临时目录 ==="
    if [ -d "bin" ]; then
        print_yellow "删除 bin 目录..."
        rm -rf bin/
    fi
    if [ -d "tmp/figure" ]; then
        print_yellow "删除 tmp/figure 目录..."
        rm -rf tmp/figure/
    fi
    if [ -d "obj" ]; then
        print_yellow "删除 obj 目录..."
        rm -rf obj/
    fi
    print_green "清理完成"
}
run_project() {
    print_green "=== 运行 YuGiOh Cards Maker ==="
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
    DOTNET_ARGS=""
    for arg in "$@"; do
        case $arg in
            --debug)
                DOTNET_ARGS="$DOTNET_ARGS --debug"
                ;;
            --png)
                DOTNET_ARGS="$DOTNET_ARGS --png"
                ;;
        esac
    done
    print_yellow "正在编译和运行 .NET 项目..."
    if [ -n "$DOTNET_ARGS" ]; then
        print_yellow "参数:$DOTNET_ARGS"
        eval "dotnet run $DOTNET_ARGS"
    else
        print_yellow "使用默认参数"
        dotnet run
    fi
}
main() {
    print_green "===== YuGiOh Cards Maker 启动脚本 ====="
    SCRIPT_DIR=$(dirname "$0")
    cd "$SCRIPT_DIR" || exit 1
    if [ ! -f "Program.cs" ] || [ ! -f "YuGiOh-Cards-Maker.csproj" ]; then
        print_red "错误: 脚本必须在项目根目录运行"
        exit 1
    fi
    install_dependencies
    create_directories
    run_project "$@"
    cleanup_directories
    print_green "===== YuGiOh Cards Maker 执行完成 ====="
}
main "$@"
