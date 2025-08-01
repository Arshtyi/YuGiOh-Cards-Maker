# YuGiOh-Cards-Maker

## 项目介绍

-   本项目是一个**自动制作简中游戏王卡片的无 GUI 工具**，若为制图请往别处
-   本项目很大程度是为了[Koishi-Plugin-YuGiOh-Cards](https://github.com/Arshtyi/koishi-plugin-yugioh-cards)及其他依赖项目
-   本仓库利用 Github Actions [Release](https://github.com/Arshtyi/YuGiOh-Cards-Maker/releases/tag/latest) 卡牌(图片压缩包以及所有数据)
-   项目在 Linux (Ubuntu24.04->Fedora41)上开发(`Shell+Python+C#`)

## 几点说明

1. 项目不是为了 DIY
2. 不会支持其他语言，卡面元素不会增加
3. 不会有高速魔法、黑暗同调、技能卡、卡背、RD 等，但是已经支持了 Token
4. 不会支持异画(懒得写适配)和非正式卡(如胜利龙)等

## 关于 Token

-   目前 token 的 id 采用公认的召唤衍生物的卡的 id+1 的方式，具体信息见 `cfg.example/`和 `res/`
-   因为 token 的信息是手动维护，所以可能存在错误

## 目录结构

-   `asset/`: 存放资源文件
    -   `figure/`: 卡片框架、图标等图片资源
    -   `font/`: 字体文件
        -   `sc/`: 简体中文字体
        -   `special/`: 特殊字体(如攻击力/守备力/Link 值/ID 字体)
-   `res/`:资源文件
    -   `other.json`:包括衍生物在内的非正式卡片
    -   `typeline.conf`: 怪兽卡的 typeline 翻译
-   `script/`: 脚本目录
    -   `process_yugioh_cards.sh`: 处理数据的脚本
    -   `extract_ids.sh`:将 other.json 的所有卡片 id 压入 `dev/debug.txt`的脚本
    -   `find_unofficial_cards.sh`:找出异画、非正式发售等卡片的 id
    -   `compare_debug_figure.sh`:比对 debug 结果
-   `cfg.example/`: 标准 JSON 示例
-   `dev/`: 开发目录
    -   `debug.txt`: Debug 处理的卡片 ID 列表
    -   同时两个 shell 脚本的输出会在此目录下
-   `log/`:日志目录(默认没有日志输出到这里)
-   `figure/`: 输出目录
-   `src/`: `C\#`程序主要代码目录
-   `tmp/`: 临时目录
    -   `cards.json`: 卡片数据
    -   `figure/`: 临时图片目录

## JSON 结构说明

采用 `C#`风格描述 JSON 格式

|         Key         |   Value Type   |                                                                           可选值                                                                            | 说明                                                | 拥有卡片                          |
| :-----------------: | :------------: | :---------------------------------------------------------------------------------------------------------------------------------------------------------: | --------------------------------------------------- | --------------------------------- |
|        name         |    `string`    |                                                                              -                                                                              | 卡名，以 cn_name 为准                               | 所有                              |
|         id          |     `int`      |                                                                              -                                                                              | 卡片 ID                                             | 所有                              |
|     description     |    `string`    |                                                                              -                                                                              | 效果描述                                            | 所有                              |
| pendulumDescription |    `string`    |                                                                              -                                                                              | 灵摆效果描述                                        | 灵摆怪兽                          |
|        scale        |     `int`      |                                                                              -                                                                              | 灵摆刻度值，没有区别左右                            | 灵摆怪兽                          |
|       linkVal       |     `int`      |                                                                              -                                                                              | 链接值                                              | 链接怪兽                          |
|     linkMarkers     | `List<string>` |                                              bottom-left,bottom,bottom-right,left,right,top-left,top,top-right                                              | 拥有的链接箭头                                      | 链接怪兽                          |
|      cardType       |    `string`    |                                                                     monster/spell/trap                                                                      | 卡片类型                                            | 所有                              |
|      attribute      |    `string`    |                                                   light/dark/divine/earth/fire/water/wind/rare/spell/trap                                                   | 卡片属性，三色和无                                  | 所有                              |
|        race         |    `string`    |                                                   normal/continuous/field/equip/quick-play/ritual/counter                                                   | 魔法陷阱种类                                        | 魔法卡，陷阱卡                    |
|         atk         |     `int`      |                                                                              -                                                                              | 怪兽的攻击力，-1 表示?                              | 怪兽卡                            |
|         def         | `int`/`string` |                                                                              -                                                                              | 怪兽的守备力，-1 表示?，链接怪兽为 null             | 怪兽卡                            |
|        level        |     `int`      |                                                                              -                                                                              | 怪兽的等级或者阶级(超量怪兽)，-1 表示没有等级和阶级 | 怪兽卡                            |
|      frameType      |    `string`    | normal/normal-pendulum/effect/effect-pendulum/fusion/fusion-pendulum/ritual/ritual-pendulum/synchro/synchro-pendulum/xyz/xyz-pendulum/link/token/spell/trap | 卡片边框类型                                        | 所有                              |
|      typeline       |    `string`    |                                                                              -                                                                              | 情报文本                                            | 怪兽卡                            |
|      cardImage      |    `string`    |                                                                              -                                                                              | 卡片中心图的文件名                                  | 默认为 id 的文本，一些 token 不同 |

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

以完成所有任务

## 可选参数

### 默认行为

-   生成所有卡图
-   删除 `tmp`目录下的中心卡图
-   生成质量为 50% 的 jpg 图片

### Shell 继承参数

这些参数能够由 shell 脚本 `YuGiOh-Cards-Maker.sh`继承给 C#程序

-   `--debug`：

    -   只生成 `dev/debug.txt`对应 ID 的卡片
    -   不删除 `tmp`目录下的卡图

-   `--png`：生成无损 png 而不是 50%质量的 jpg.

## Thx

-   [YGOProDeck](https://ygoprodeck.com/)
-   [YGOCDB](https://ygocdb.com/)
-   [MyCard](https://github.com/mycard)
-   [canvas-yugioh-card](https://github.com/kooriookami/yugioh-card)
