# YuGiOh-Cards-Maker

简中游戏王卡片生成项目,因传播造成的版权问题与开发者无关

> 目前本项目已经足够完善，未来不会进行大规模改动(不包括上游)，可以放心使用
>
> 除非 Konami 未来推出新的召唤法或者 pendulum-link,否则本项目不会有 breaking changes
>
> 上游可能还会有 breaking changes

## 项目介绍

> 我认为很有必要说明本项目的意义，其实已经存在非常多的制卡器供大家选择，但是这些制卡器几乎都没有提供利于自动化的方式，所以才有了这个项目
>
> 但是我的需求足够少，因此我不会主动为这个项目引入过多的额外要素
>
> **这个项目不是为了 DIY 而存在的**
>
> 同时需要说明的是，许多参数的值与后续调整我并没有更多的测试，也许他们并不见得合适，并且不可能像 PS 一样精致
>
> 关于中心卡图，其实我考虑过进行超分，但是因为需要协调工作流，我放弃了
>
> 如果你有更进一步的想法，欢迎联系我，方式见[Contact Me](#contact-me)
>
> 祝大家决斗愉快

-   本项目是一个**自动制作简中游戏王卡片图片及数据信息的无 GUI 工具**,若为 DIY 向制图请往别处
-   本项目很大程度是为了[Koishi-Plugin-YuGiOh-Cards](https://github.com/Arshtyi/koishi-plugin-yugioh-cards)及其他依赖项目
-   本项目利用 Github Actions [Release](https://github.com/Arshtyi/YuGiOh-Cards-Maker/releases/tag/latest) 卡牌(图片压缩包以及所有数据)
-   本项目在 Linux 上开发
-   项目语言:`Python3 + Shell + C# + Docker`

## 项目上游

1. 衍生物信息来源于[YuGiOh-Tokens](https://github.com/Arshtyi/YuGiOh-Tokens)仓库
2. 种族翻译信息来源于[Translations-Of-YuGiOh-Cards-Type](https://github.com/Arshtyi/Translations-Of-YuGiOh-Cards-Type)仓库
3. 禁限卡表信息来源于[YuGiOh-Forbidden-And-Limited-List](https://github.com/Arshtyi/YuGiOh-Forbidden-And-Limited-List)仓库
4. 依赖与数据源头见[THX](#thx)

## 项目说明

1. **项目不是为了 DIY**,作者也不会主动进行 DIY 适配
2. 不会主动做 windows 适配,但是已经有了 Docker 支持
3. 不会支持其他卡面语言,卡面元素不会增加(包括但不限于卡包、水印、角标)
4. 不会有高速魔法、黑暗同调、技能卡、卡背、RD 等,但是已经支持了 Token
5. 不会支持异画(~~懒得写适配~~，这牵涉到上下游的数据格式兼容与处理问题)和非正式卡(如观赏卡)等
6. 所有卡片的描述采用 YGOPRO 风格
7. _禁限卡表支持且仅支持 OCG/TCG/MD_
8. 由于 [YGOProDeck](https://ygoprodeck.com/)与[YGOCDB](https://ygocdb.com/)两个数据上游的更新速度不同，本项目的卡图生成几乎不可能做到 100%成功，这是非常正常的(也就是`log/failure.txt`文件几乎一定不为空，具体将在 [release](https://github.com/Arshtyi/YuGiOh-Cards-Maker/releases/tag/latest) 页体现)

## 关于 Token

-   衍生物信息来源于[YuGiOh-Tokens](https://github.com/Arshtyi/YuGiOh-Tokens)仓库
-   目前 token 的 id 采用公认的召唤衍生物的卡的 id+1 的方式,具体信息见 `cfg.example/`和 `res/`
-   因为 token 的信息是**手动维护**,所以可能存在错误

## 项目结构

-   `asset/`: 存放资源文件
    -   `figure/`: 卡片框架、图标等图片资源
    -   `font/`: 字体文件
        -   `sc/`: 简体中文字体
        -   `special/`: 特殊字体(如攻击力/守备力/Link 值/ID 字体)
-   `res/`:资源文件,已经被切割为上游提供支撑
    -   `typeline.conf`: 怪兽卡的 typeline 翻译
    -   `token.json`:衍生物信息
    -   `limit/`:禁限卡表
        -   `ocg.json`:ocg 禁限卡表
        -   `tcg.json`:tcg 禁限卡表
        -   `md.json`:md 禁限卡表
-   `script/`: 脚本目录
    -   `process_yugioh_cards.sh`: 处理数据的脚本
    -   `extract_ids.sh`:将 token.json 的所有卡片 id 压入 `dev/debug.txt`的脚本
    -   `find_unofficial_cards.sh`:找出异画、非正式发售卡等卡片的 id
    -   `compare_debug_figure.sh`:比对 debug 结果
-   `cfg.example/`: 标准 JSON 示例
    -   `monster.json`:怪兽标准 json
    -   `link.json`:链接怪兽标准 json
    -   `pendulum.json`:灵摆怪兽标准 json
    -   `token.json`:衍生物标准 json
    -   `spell.json`:魔法标准 json
    -   `trap.json`:陷阱标准 json
-   `dev/`: 开发目录
    -   `debug.txt`: Debug 处理的卡片 ID 列表
    -   同时一些 `script/`下 shell 脚本的输出会在此目录下(主要用于调试和比对一些结果)
-   `log/`:日志目录
    -   `failure.txt`:所有失败的卡片 id 与原因
-   `figure/`: 卡图输出目录
-   `src/`: C#程序主要代码目录
    -   `card.cs`:卡片类定义
    -   `cardGenerator.cs`:卡片生成
    -   `stringNumericConverter.cs`:字符转化
-   `tmp/`: 临时目录
    -   `cards.json`: 卡片数据
    -   `figure/`: 临时图片目录
-   `process_yugioh_cards.py`:负责整合数据得到 `tmp/cards.json`
-   `Program.cs`:生图程序入口
-   `YuGiOh-Cards-Maker.csproj`:C#环境依赖
-   `YuGiOh-Cards-Maker.sh`:一键脚本
-   `Dockerfile`:Docker 构建文件
-   `entrypoint.sh`:命令行参数传递脚本

## JSON 结构说明

采用 C#风格描述 JSON 格式

|         Key         |             Value Type              |                                                                            可选值                                                                             | 说明                                               | 拥有卡片                         |
| :-----------------: | :---------------------------------: | :-----------------------------------------------------------------------------------------------------------------------------------------------------------: | -------------------------------------------------- | -------------------------------- |
|        name         |              `string`               |                                                                               -                                                                               | 卡名,以 cn_name 为准                               | 所有                             |
|         id          |                `int`                |                                                                               -                                                                               | 卡片 ID                                            | 所有                             |
|     description     |              `string`               |                                                                               -                                                                               | 效果描述                                           | 所有                             |
| pendulumDescription |              `string`               |                                                                               -                                                                               | 灵摆效果描述                                       | 灵摆怪兽                         |
|        scale        |                `int`                |                                                                               -                                                                               | 灵摆刻度值,没有区别左右                            | 灵摆怪兽                         |
|       linkVal       |                `int`                |                                                                               -                                                                               | 链接值                                             | 链接怪兽                         |
|     linkMarkers     |           `List<string>`            |                                              `bottom-left,bottom,bottom-right,left,right,top-left,top,top-right`                                              | 拥有的链接箭头                                     | 链接怪兽                         |
|      cardType       |              `string`               |                                                                     `monster/spell/trap`                                                                      | 卡片类型                                           | 所有                             |
|      attribute      |              `string`               |                                                   `light/dark/divine/earth/fire/water/wind/rare/spell/trap`                                                   | 卡片属性,三色和无                                  | 所有                             |
|        race         |              `string`               |                                                   `normal/continuous/field/equip/quick-play/ritual/counter`                                                   | 魔法陷阱种类                                       | 魔法卡,陷阱卡                    |
|         atk         |                `int`                |                                                                               -                                                                               | 怪兽的攻击力,-1 表示?                              | 怪兽卡                           |
|         def         |           `int`/`string`            |                                                                               -                                                                               | 怪兽的守备力,-1 表示?,链接怪兽为 null              | 怪兽卡                           |
|        level        |                `int`                |                                                                               -                                                                               | 怪兽的等级或者阶级(超量怪兽),-1 表示没有等级和阶级 | 怪兽卡                           |
|      frameType      |              `string`               | `normal/normal-pendulum/effect/effect-pendulum/fusion/fusion-pendulum/ritual/ritual-pendulum/synchro/synchro-pendulum/xyz/xyz-pendulum/link/token/spell/trap` | 卡片边框类型                                       | 所有                             |
|      typeline       |              `string`               |                                                                               -                                                                               | 情报文本                                           | 怪兽卡                           |
|        limit        | `List<Dictionary<string, object?>>` |      `["ocg":null/"forbidden"/"limited"/"semi-limited","tcg":null/"forbidden"/"limited"/"semi-limited", "md":null/"forbidden"/"limited"/"semi-limited"]`      | 禁限情况,包括 OCG、TCG、MD                         | 所有                             |
|      cardImage      |              `string`               |                                                                               -                                                                               | 卡片中心图的文件名                                 | 默认为 id 的文本,一些 token 不同 |

## 本地

### 安装依赖

```bash
# 安装 .NET SDK
sudo dnf update && sudo dnf install dotnet-sdk-8.0
# 恢复项目依赖
dotnet restore
```

### 获取和更新数据

```bash
./script/process_yugioh_cards.sh
```

### 编译和运行

```bash
dotnet build && dotnet run
```

### 一键运行

直接运行

```bash
./YuGiOh-Cards-Maker.sh
```

以完成所有流程

## Docker

### 构建

```bash
docker build -t yugioh-cards-maker:latest .
```

### 运行

```bash
docker run -it yugioh-cards-maker:latest
```

### 复制

```bash
docker ps -a
docker cp <CONTAINER_ID>:/app/figure ./figure
```

> ```bash
> # 拉取最新镜像
> docker pull arshtyier/yugioh-cards-maker:latest
> # 运行
> docker run -it arshtyier/yugioh-cards-maker:latest
> ```

## 可选参数

### 默认行为

-   生成所有卡图
-   删除 `tmp`目录下的中心卡图
-   生成质量为 50% 的 jpg 图片

### Shell 继承参数

这些参数能够由 shell 脚本 `YuGiOh-Cards-Maker.sh`继承给 C# 程序,Docker 则追加到 run 命令之后即可.

1.  `--debug`：

    -   只生成 `dev/debug.txt`对应 ID 的卡片
    -   不删除 `tmp`目录下的卡图

2.  `--png`：生成无损 png 而不是 50%质量的 jpg.

## Contact me

-   repo:[YuGiOh-Cards-Maker](https://github.com/Arshtyi/YuGiOh-Cards-Maker)
-   E-mail:arshtyi@foxmail.com
-   QQ:64006128

## Thx

### Code

-   ChatGPT
-   Claude

### Font

-   [霞鹜文楷](https://github.com/lxgw/LxgwWenKai)
-   ygo-atk-def、ygo-link、ygo-password(抱歉，此三者我并没有找到作者信息)

### API

-   [YGOProDeck](https://ygoprodeck.com/)
-   [YGOCDB](https://ygocdb.com/)

### 禁限卡表

-   [Yugipedia](https://yugipedia.com/wiki/Yugipedia)

### 制图思路

-   [MyCard](https://github.com/mycard)
-   [Canvas-YuGiOh-Cards](https://github.com/kooriookami/yugioh-card)

### 其他

感谢赤子奈落开发的 MDPro3,不管是在寻找制图思路还是在维护衍生物信息的过程中，我都参考了此项目的一些思路和结果

同时也要感谢 YGOPro 和 MyCard 历年来所有的开发者、维护者

最后要感谢 CNYGO 的所有贡献者

但是，**FUCK KONAMI**
