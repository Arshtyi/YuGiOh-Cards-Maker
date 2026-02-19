# YuGiOh-Cards-Maker

- 简中游戏王卡片生成项目
- 因传播造成的版权问题与开发者无关

## 项目介绍

- 本项目是一个**自动制作简中游戏王卡片图片及数据信息的无 GUI 工具**，若为 DIY 向制图请往别处
- 本项目利用 Github Actions [Release](https://github.com/Arshtyi/YuGiOh-Cards-Maker/releases/tag/latest) 卡牌(图片压缩包以及所有数据)

## 项目上游

1.  衍生物信息:[Arshtyi/YuGiOh-Tokens](https://github.com/Arshtyi/YuGiOh-Tokens)
2.  种族翻译:[Arshtyi/Translations-Of-YuGiOh-Cards-Type](https://github.com/Arshtyi/Translations-Of-YuGiOh-Cards-Type)
3.  禁限卡表:[Arshtyi/YuGiOh-Forbidden-And-Limited-List](https://github.com/Arshtyi/YuGiOh-Forbidden-And-Limited-List)
4.  卡模素材:[Arshtyi/Card-Templates-Of-YuGiOh](https://github.com/Arshtyi/Card-Templates-Of-YuGiOh)
5.  卡牌数据和中心图:[Arshtyi/YuGiOh-Cards-Asset](https://github.com/Arshtyi/YuGiOh-Cards-Asset)
6.  依赖与数据源头见[THX](#thx)

## 项目说明

1. 项目不是为了 DIY，作者也不会主动进行 DIY 适配
2. 本项目仅专注于 OCG、TCG 实卡和 MD 已上线的卡牌，未 O 化卡暂时不考虑（比如 link-spell）
3. 不会支持其他卡面语言，卡面元素不会增加(包括但不限于卡包、水印、角标)
4. 不会有高速魔法、黑暗同调、技能卡、卡背、RD 等，但是支持 Token
5. 所有卡片的描述采用 YGOPRO 风格
6. 禁限卡表支持且仅支持 OCG/TCG/MD
7. container 采用 github actions 自动构建并发布于[packages](https://github.com/Arshtyi/YuGiOh-Cards-Maker/pkgs/container/yugioh-cards-maker)

## Usage

### Global

```bash
## download resource
chmod +x script/download_resources.sh
./script/download_resources.sh

## build
sudo dnf update && sudo dnf install dotnet-sdk-8.0
dotnet restore
dotnet build

## run
dotnet run
```

or

```bash
chmod +x YuGiOh-Cards-Maker.sh
./YuGiOh-Cards-Maker.sh
```

### Docker

```bash
# build & run
docker build -t yugioh-cards-maker:latest .
docker run -it yugioh-cards-maker:latest

# get output
docker ps -a
docker cp <CONTAINER_ID>:/app/figure ./figure
```

or

```bash
docker pull ghcr.io/arshtyi/yugioh-cards-maker:latest
```

## 可选参数

### 默认行为

- 尽可能生成`cards.json`所有卡图
- 删除`tmp/figure`下的中心图
- 生成质量为 50% 的 jpg 图片

### Shell 继承参数

这些参数能够由 shell 脚本 `YuGiOh-Cards-Maker.sh`继承给 C# 程序,Docker 则追加到 run 命令之后即可.

1.  `--debug`：
    - 只生成 `dev/debug.txt`对应 ID 的卡片
    - 不清理各中间目录

2.  `--png`：生成无损 png 而不是 50%质量的 jpg.

## Contact me

- E-mail:arshtyi@foxmail.com
- QQ:64006128

## Thx

### Code

- ChatGPT
- Claude

### 卡模

- 感谢白羽幸鳥制作的卡模
- 关于卡模的更多信息请访问[Arshtyi/Card-Templates-Of-YuGiOh](https://github.com/Arshtyi/Card-Templates-Of-YuGiOh)

### Font

- 包括华康楷体在内的诸多字体（大部分已找不到源头）
- [霞鹜文楷](https://github.com/lxgw/LxgwWenKai)

关于字体的更多信息请访问[Arshtyi/Card-Templates-Of-YuGiOh](https://github.com/Arshtyi/Card-Templates-Of-YuGiOh)

### API

- [YGOProDeck](https://ygoprodeck.com/)
- [YGOCDB](https://ygocdb.com/)

### 禁限卡表

- [Yugipedia](https://yugipedia.com/wiki/Yugipedia)

### 制图思路

- [MyCard](https://github.com/mycard)
- [Canvas-YuGiOh-Cards](https://github.com/kooriookami/yugioh-card)

### RHS

感谢赤子奈落开发的 [MDPro3](https://code.moenext.com/sherry_chaos/MDPro3),不管是在寻找制图思路还是在维护衍生物信息的过程中，我都参考了此项目的一些思路和结果

同时也要感谢 YGOPro 和 MyCard 历年来所有的开发者、维护者

然后要感谢 YGODIY 这一过程中所有贡献者，正是前人的工作让我能够更顺利地开发

最后要感谢 CNYGO 的所有贡献者

但是，**FUCK KONAMI**
