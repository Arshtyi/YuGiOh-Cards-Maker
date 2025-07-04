# YuGiOh-Cards-Maker

## 项目介绍

-   本项目是一个自动制作**简中游戏王卡片的无 GUI 工具**，若为制图请往别处
-   本项目很大程度是为了[Terminus](https://github.com/Arshtyi/Terminus)及其他依赖项目
-   本仓库利用 Github Actions Release 卡牌（图片压缩包以及所有数据)
-   项目在 Linux (Ubuntu24.04->Fedora41)上开发（`Shell+Python+C#`）

## 几点说明

1. 项目不是为了 DIY
2. 不会支持其他语言
3. 卡面元素暂时不会增加
4. 不会有高速魔法、黑暗同调、技能卡、卡背、RD 等，但是支持 Token
5. 不会支持异画（懒得写适配)和非正式卡等

## 关于 Token

-   目前 token 的 id 采用公认的召唤衍生物的卡的 id+1 的方式，具体信息见 `cfg/`和 `res/`
-   因为 token 的信息是手动维护，所以可能存在错误

## 目录结构

-   `asset/`: 存放资源文件
    -   `figure/`: 卡片框架、图标等图片资源
    -   `font/`: 字体文件
        -   `sc/`: 简体中文字体
        -   `special/`: 特殊字体（如攻击力/守备力/Link 值/ID 字体）
-   `res/`:资源文件
    -   `other.json`:包括衍生物在内的非正式卡片
    -   `typeline.conf`: 怪兽卡的 typeline 翻译
-   `script/`: 脚本目录
    -   `process_yugioh_cards.sh`: 处理数据的脚本
    -   `extract_ids.sh`:将 other.json 的所有卡片 id 压入 `dev/debug.txt`的脚本
    -   `find_unofficial_cards.sh`:找出异画、非正式发售等卡片的 id
    -   `compare_debug_figure.sh`:比对 debug 结果
-   `cfg/`: 标准 JSON 示例
-   `dev/`: 开发目录
    -   `debug.txt`: Debug 处理的卡片 ID 列表
    -   同时两个 shell 脚本的输出会在此目录下
-   `log/`:日志目录（默认没有日志输出到这里）
-   `figure/`: 输出目录
-   `src/`: 代码目录
-   `tmp/`: 临时目录
    -   `cards.json`: 卡片数据
    -   `figure/`: 临时图片目录

## 获取数据

```bash
./script/process_yugioh_cards.sh
```

## 安装依赖

```bash
# 安装 .NET SDK
sudo dnf update && sudo dnf install dotnet-sdk-8.0
# 恢复项目依赖
dotnet restore
```

## 编译和运行

```bash
dotnet run
```

或者直接运行

```bash
./YuGiOh-Cards-Maker.sh
```

## 可选参数

### Shell 继承参数

这些参数能够由 shell 脚本 `YuGiOh-Cards-Maker.sh`继承给 C#程序.

-   `--debug`：

    -   只生成 `dev/debug.txt`对应 ID 的卡片
    -   不删除 `tmp`目录下的卡图

-   `--png`：生成无损 png 而不是 50%质量的 jpg.

## Thx

-   [YGOProDeck](https://ygoprodeck.com/)
-   [百鸽](https://ygocdb.com/)
-   [MyCard](https://github.com/mycard)
-   [canvas-yugioh-card](https://github.com/kooriookami/yugioh-card)
