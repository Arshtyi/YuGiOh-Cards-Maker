# YuGiOh-Cards-Maker

## 项目介绍

-   本项目是一个自动制作简中游戏王卡片的无 GUI 工具，若想要做制图请往别处
-   本项目很大程度是为了[Terminus](https://github.com/Arshtyi/Terminus)
-   本仓库会利用 Github Actions Release 卡牌压缩包（为了达到这一点，采用了生成 jpg 而不是 png 的方式)
-   项目在 Linux 上开发（`Shell+Python+C#`）

## 环境配置

-   \>=.NET 8.0

### 目录结构

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
-   `cfg/`: 标准 JSON
-   `figure/`: 输出目录
-   `src/`: 代码目录

### 安装依赖

```bash
# 安装 .NET SDK
sudo apt update && sudo apt install dotnet-sdk-8.0
# 恢复项目依赖
dotnet restore
```

### 编译和运行

```bash
dotnet build && dotnet run
```

或者直接运行

```bash
./YuGiOh-Cards-Maker.sh
```

## Thx

-   [YGOProDeck](https://ygoprodeck.com/)
-   [百鸽](https://ygocdb.com/)
-   [MyCard](https://github.com/mycard)
-   [canvas-yugioh-card](https://github.com/kooriookami/yugioh-card)
