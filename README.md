# YuGiOh-Cards-Maker

## 项目介绍

-   本项目是一个自动制作**简中游戏王卡片的无 GUI 工具**，若想要做制图请往别处
-   本项目很大程度是为了[Terminus](https://github.com/Arshtyi/Terminus)
-   本仓库利用 Github Actions Release 卡牌
-   项目在 Linux 上开发（`Shell+Python+C#`）

## 几点说明

1. 项目不是为了 DIY
2. 不会支持其他语言
3. 卡面元素暂时不会增加

## 目录结构

-   `asset/`: 存放资源文件
    -   `figure/`: 卡片框架、图标等图片资源
    -   `font/`: 字体文件
        -   `sc/`: 简体中文字体
        -   `special/`: 特殊字体（如攻击力/守备力/Link 值/ID 字体）
-   `res/`:资源文件
    -   `other.json`:包括衍生物在内的非正式卡片
    -   `figure/`: 上述非正式卡的图片资源，将会被复制到 `tmp/figure`
    -   `typeline.conf`: 怪兽卡的 typeline 翻译
-   `script/`: 脚本目录
    -   `process_yugioh_cards.sh`: 处理数据的脚本
-   `cfg/`: 标准 JSON 示例
-   `dev/`: 开发目录
    -   `debug.txt`: Debug 处理的卡片 ID 列表
-   `log/`:日志目录（默认没有日志输出)
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
sudo apt update && sudo apt install dotnet-sdk-8.0
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

## DEBUG

使用如下命令

```bash
dotnet run debug
```

使得程序

-   只生成 `dev/debug.txt`对应 ID 的卡片
-   不删除卡图

这样便于测试

## Thx

-   [YGOProDeck](https://ygoprodeck.com/)
-   [百鸽](https://ygocdb.com/)
-   [MyCard](https://github.com/mycard)
-   [canvas-yugioh-card](https://github.com/kooriookami/yugioh-card)
