using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using System.Numerics;
namespace Yugioh
{
    public static class CardGenerator
    {
        private static readonly RectangleF CardNameArea = new RectangleF(89.86f, 96.10f, 1113.00f - 89.86f, 224.71f - 96.10f);
        private static readonly RectangleF PendulumDescriptionArea = new RectangleF(220f, 1300f, 1180f - 220f, 1500f - 1300f);
        private static readonly RectangleF CardDescriptionArea = new RectangleF(110f, 1533f, 1283f - 110f, 1897f - 1533f);
        private static readonly string FontPath = Path.Combine("asset", "font", "sc", "XinHuaKaiTi.ttf");
        // 资源目录
        private static readonly string CardsDir = "cards";
        private static readonly string MasksDir = "masks";
        private static readonly string AttributesDir = "attributes";
        private static readonly string IndicatorsDir = "indicators";
        private static readonly string IconsDir = "icons";
        private static readonly string ArrowsDir = "arrows";
        private static Font? nameBlackFont;
        private static Font? nameWhiteFont;
        private static Color nameBlackColor;
        private static Color nameWhiteColor;
        private static Color nameShadowColor;
        private static FontFamily fontFamily;
        private static FontCollection LoadFonts()
        {
            var fontCollection = new FontCollection();
            try
            {
                var fontFamily = fontCollection.Add(FontPath);
                CardGenerator.fontFamily = fontFamily;
                float fontSize = 95f;
                nameBlackFont = fontFamily.CreateFont(fontSize, FontStyle.Regular);
                nameWhiteFont = fontFamily.CreateFont(fontSize, FontStyle.Regular);
                nameBlackColor = Color.Black;
                nameWhiteColor = Color.White;
                nameShadowColor = Color.FromRgba(0, 0, 0, 80);
                return fontCollection;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载字体失败: {ex.Message}");
                throw;
            }
        }
        public static void GenerateCards(string cardsJsonPath, string assetFigureDir, string outputFigureDir, bool debug = false, bool usePng = false)
        {
            Console.WriteLine("开始卡片图像生成...");
            if (!File.Exists(cardsJsonPath))
            {
                Console.WriteLine($"错误: 未找到卡片数据文件: {cardsJsonPath}");
                return;
            }
            // 清空输出
            if (Directory.Exists(outputFigureDir))
            {
                Directory.CreateDirectory(outputFigureDir);
                foreach (var file in Directory.GetFiles(outputFigureDir, "*.jpg")
                    .Concat(Directory.GetFiles(outputFigureDir, "*.png")))
                {
                    File.Delete(file);
                }
            }
            else
            {
                Directory.CreateDirectory(outputFigureDir);
            }
            var json = File.ReadAllText(cardsJsonPath);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var dict = JsonSerializer.Deserialize<Dictionary<string, Card>>(json, jsonOptions);
            if (dict == null)
            {
                Console.WriteLine("错误: 卡片数据解析失败");
                return;
            }
            var allValidCards = dict.Values.Where(c => !string.IsNullOrEmpty(c.FrameType)).ToList();
            List<Card> cardsToProcess;
            if (debug)
            {
                string debugFilePath = Path.Combine("dev", "debug.txt");
                if (File.Exists(debugFilePath))
                {
                    var debugIds = File.ReadAllLines(debugFilePath)
                        .Where(line => !string.IsNullOrWhiteSpace(line))
                        .Select(line => line.Trim())
                        .ToList();
                    cardsToProcess = new List<Card>();
                    foreach (var idStr in debugIds)
                    {
                        if (dict.TryGetValue(idStr, out Card? card) && card != null && !string.IsNullOrEmpty(card.FrameType))
                        {
                            cardsToProcess.Add(card);
                        }
                        else
                        {
                            Console.WriteLine($"警告: debug.txt中的ID {idStr} 在cards.json中不存在或没有有效的框架类型");
                        }
                    }
                    Console.WriteLine($"将仅处理debug.txt中指定的{cardsToProcess.Count}张卡片");
                    Console.WriteLine("Debug模式下不会删除tmp/figure目录下的原始PNG文件");
                    if (cardsToProcess.Count == 0)
                    {
                        Console.WriteLine("警告: debug.txt中没有匹配到任何有效的ID");
                    }
                }
                else
                {
                    Console.WriteLine($"警告: 未找到debug文件: {debugFilePath}, 将退出处理");
                    return;
                }
            }
            else
            {
                cardsToProcess = allValidCards;
                Console.WriteLine($"将处理全部{allValidCards.Count}张卡片");
            }
            LoadFonts();
            // 并行处理
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 500 };
            int processed = 0;
            int failed = 0;
            Parallel.ForEach(cardsToProcess, parallelOptions, card =>
            {
                try
                {
                    var frameType = card.FrameType?.ToLower() ?? "normal";
                    string? frameFile = null;
                    string exactFramePath = Path.Combine(assetFigureDir, CardsDir, $"card-{frameType}.png");
                    if (File.Exists(exactFramePath))
                    {
                        frameFile = exactFramePath;
                    }
                    if (frameFile == null)
                    {
                        Console.WriteLine($"错误: 无法找到卡框,ID: {card.Id}, 名称: {card.Name}, 框架类型: {frameType}");
                        Interlocked.Increment(ref failed);
                        return;
                    }
                    // 根据参数决定输出文件扩展名
                    string fileExtension = usePng ? ".png" : ".jpg";
                    var outPath = Path.Combine(outputFigureDir, $"{card.Id}{fileExtension}");
                    using (var image = Image.Load(frameFile))
                    {
                        // 属性
                        AddAttributeImage(image, card, assetFigureDir);
                        // 卡图
                        AddCardImage(image, card, "tmp/figure");
                        // 灵摆卡->灵摆框
                        if (frameType.Contains("pendulum"))
                        {
                            string pendulumMaskPath = Path.Combine(assetFigureDir, MasksDir, "card-mask-pendulum.png");
                            if (File.Exists(pendulumMaskPath))
                            {
                                using (var pendulumMask = Image.Load(pendulumMaskPath))
                                {
                                    int maskX = 70;
                                    int maskY = 354;
                                    image.Mutate(ctx => ctx.DrawImage(pendulumMask, new Point(maskX, maskY), 1f));
                                }
                            }
                        }
                        // 非灵摆卡->普通框
                        else
                        {
                            string maskPath = Path.Combine(assetFigureDir, MasksDir, "card-mask.png");
                            if (File.Exists(maskPath))
                            {
                                using (var cardMask = Image.Load(maskPath))
                                {
                                    int maskX = 125;
                                    int maskY = 322;
                                    image.Mutate(ctx => ctx.DrawImage(cardMask, new Point(maskX, maskY), 1f));
                                }
                            }
                            else
                            {
                                Console.WriteLine($"警告: 未找到卡框文件: {maskPath}");
                            }
                        }
                        // 攻守条
                        AddAtkDefImage(image, card, assetFigureDir);
                        // 灵摆刻度
                        AddPendulumScale(image, card);
                        // 星级/阶级图标
                        AddLevelOrRank(image, card, assetFigureDir);
                        // Link箭头
                        AddLinkArrows(image, card, assetFigureDir);
                        bool isXyzMonster = frameType.Contains("xyz");
                        bool isSpellOrTrap = card.CardType?.ToLower() == "spell" || card.CardType?.ToLower() == "trap";
                        bool isSpecialCard = isXyzMonster || isSpellOrTrap;
                        // 卡名
                        DrawCardName(image, card.Name, isSpecialCard);
                        // ID
                        AddCardID(image, card);
                        // 魔法卡/陷阱卡类型文字
                        AddCardTypeText(image, card);
                        // 灵摆效果
                        DrawPendulumDescription(image, card);
                        // 卡牌效果
                        DrawCardDescription(image, card);
                        // 根据参数保存
                        if (usePng)
                        {
                            // 保存为无损PNG
                            var pngEncoder = new PngEncoder
                            {
                                CompressionLevel = PngCompressionLevel.BestCompression
                            };
                            image.Save(outPath, pngEncoder);
                        }
                        else
                        {
                            // 保存为JPG（质量50%）
                            var jpegEncoder = new JpegEncoder
                            {
                                Quality = 50
                            };
                            image.Save(outPath, jpegEncoder);
                        }
                        // 在非debug模式下删除临时目录中的原始PNG
                        if (!debug)
                        {
                            string tmpPngPath = Path.Combine("tmp/figure", $"{card.Id}.png");
                            if (File.Exists(tmpPngPath))
                            {
                                try
                                {
                                    File.Delete(tmpPngPath);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"警告: 无法删除临时PNG: {tmpPngPath}, 错误: {ex.Message}");
                                }
                            }
                        }
                    }
                    Interlocked.Increment(ref processed);
                    if (processed % 100 == 0)
                    {
                        Console.WriteLine($"已处理: {processed}/{cardsToProcess.Count} 张卡片");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"处理卡片失败: ID={card.Id}, 名称={card.Name}, 错误: {ex.Message}");
                    Interlocked.Increment(ref failed);
                }
            });
            Console.WriteLine($"卡片生成完成！成功: {processed}, 失败: {failed}");
            Console.WriteLine($"输出目录: {Path.GetFullPath(outputFigureDir)}");
        }
        // 卡名
        private static void DrawCardName(Image image, string cardName, bool isSpecialCard)
        {
            try
            {
                float fontSize = 95f;
                float posYOffset = 30f;
                bool hasSpecialSeparator = cardName.Contains("·") || cardName.Contains("-") || cardName.Contains("・");
                float effectiveLength = CalculateEffectiveLength(cardName);
                if (effectiveLength <= 10)
                {
                    fontSize = 105f;
                    posYOffset = 25f;
                }
                else if (effectiveLength <= 12)
                {
                    fontSize = 88f;
                    posYOffset = 30f;
                }
                else if (effectiveLength <= 14)
                {
                    fontSize = 75f;
                    posYOffset = 35f;
                }
                else if (effectiveLength <= 16)
                {
                    fontSize = 70f;
                    posYOffset = 40f;
                }
                else if (effectiveLength <= 18)
                {
                    fontSize = 59f;
                    posYOffset = 43f;
                }
                else if (effectiveLength <= 20)
                {
                    fontSize = 55f;
                    posYOffset = 45f;
                }
                else if (effectiveLength == 21)
                {
                    fontSize = 50f;
                    posYOffset = 45f;
                }
                else if (effectiveLength > 21)
                {
                    fontSize = 46f;
                    posYOffset = 48f;
                }
                Font nameFont = fontFamily.CreateFont(fontSize, FontStyle.Regular);
                Color textColor = isSpecialCard ? nameWhiteColor : nameBlackColor;
                float posX = CardNameArea.X + 20f;
                float posY = CardNameArea.Y + posYOffset;
                image.Mutate(ctx =>
                {
                    ctx.DrawText(cardName, nameFont, nameShadowColor, new PointF(posX + 3f, posY + 3f));
                    ctx.DrawText(cardName, nameFont, textColor, new PointF(posX, posY));
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"绘制卡名失败: {ex.Message}");
            }
        }
        // 计算卡名有效长度
        private static float CalculateEffectiveLength(string cardName)
        {
            float effectiveLength = 0;
            foreach (char c in cardName)
            {
                if (IsLatinCharacter(c))
                {
                    // 英文字母、数字和窄符号计为0.5个字符长度
                    effectiveLength += 0.5f;
                }
                else if (IsSpecialSeparator(c))
                {
                    // 特殊分隔符计为0.7个字符长度
                    effectiveLength += 0.7f;
                }
                else
                {
                    // 中文汉字、日文假名等宽字符计为1个字符长度
                    effectiveLength += 1.0f;
                }
            }
            return effectiveLength;
        }
        // 添加攻守条
        private static void AddAtkDefImage(Image image, Card card, string assetFigureDir)
        {
            try
            {
                var frameType = card.FrameType?.ToLower() ?? "";
                bool isMonsterCard = card.CardType?.ToLower() == "monster";
                if (isMonsterCard)
                {
                    string atkDefImageName = card.LinkValue.HasValue && card.LinkValue.Value > 0 ? "atk-link.png" : "atk-def.png";
                    string atkDefImagePath = Path.Combine(assetFigureDir, IndicatorsDir, atkDefImageName);
                    if (File.Exists(atkDefImagePath))
                    {
                        using (var atkDefImage = Image.Load(atkDefImagePath))
                        {
                            int posX = 106;
                            int posY = 1854;
                            image.Mutate(ctx => ctx.DrawImage(atkDefImage, new Point(posX, posY), 1f));
                        }
                        // 攻击力和守备力/Link值
                        AddAtkDefValues(image, card);
                    }
                    else
                    {
                        Console.WriteLine($"错误: 未找到攻守条文件: {atkDefImagePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"添加攻守条失败: {ex.Message}");
            }
        }
        // 属性
        private static void AddAttributeImage(Image image, Card card, string assetFigureDir)
        {
            try
            {
                if (string.IsNullOrEmpty(card.Attribute))
                {
                    return;
                }
                string attributeImageName = $"attribute-{card.Attribute.ToLower()}.png";
                string attributeImagePath = Path.Combine(assetFigureDir, AttributesDir, attributeImageName);
                if (File.Exists(attributeImagePath))
                {
                    using (var attributeImage = Image.Load(attributeImagePath))
                    {
                        int posX = 1170;
                        int posY = 95;
                        image.Mutate(ctx => ctx.DrawImage(attributeImage, new Point(posX, posY), 1f));
                    }
                }
                else
                {
                    Console.WriteLine($"错误: 未找到属性图像文件: {attributeImagePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"添加属性图像失败: {ex.Message}");
            }
        }
        // 灵摆刻度
        private static void AddPendulumScale(Image image, Card card)
        {
            try
            {
                var frameType = card.FrameType?.ToLower() ?? "";
                if (!frameType.Contains("pendulum") || !card.PendulumScale.HasValue)
                {
                    return;
                }
                string pendulumFontPath = Path.Combine("asset", "font", "special", "ygo-atk-def.ttf");
                if (!File.Exists(pendulumFontPath))
                {
                    Console.WriteLine($"错误: 未找到灵摆刻度字体文件: {pendulumFontPath}");
                    return;
                }
                var fontCollection = new FontCollection();
                var pendulumFontFamily = fontCollection.Add(pendulumFontPath);
                var pendulumFont = pendulumFontFamily.CreateFont(90f, FontStyle.Bold);
                var color = Color.Black;
                int leftScaleX = 122;
                int leftScaleY = 1415;
                int rightScaleX = 1230;
                int rightScaleY = 1415;
                string scaleText = card.PendulumScale.Value.ToString();
                int offsetX = 0;
                if (scaleText.Length > 1)
                {
                    offsetX = 26;
                }
                image.Mutate(ctx => ctx.DrawText(scaleText, pendulumFont, color, new PointF(leftScaleX - offsetX, leftScaleY)));
                image.Mutate(ctx => ctx.DrawText(scaleText, pendulumFont, color, new PointF(rightScaleX - offsetX, rightScaleY)));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"添加灵摆刻度失败: {ex.Message}");
            }
        }
        // 星级/阶级
        private static void AddLevelOrRank(Image image, Card card, string assetFigureDir)
        {
            try
            {
                var frameType = card.FrameType?.ToLower() ?? "";
                bool isMonsterCard = card.CardType?.ToLower() == "monster";
                if (!isMonsterCard || !card.Level.HasValue || card.Level.Value == -1)
                {
                    return;
                }
                bool isXyz = frameType.Contains("xyz");
                string iconFileName = isXyz ? "rank.png" : "level.png";
                string iconFilePath = Path.Combine(assetFigureDir, IndicatorsDir, iconFileName);
                if (!File.Exists(iconFilePath))
                {
                    Console.WriteLine($"错误: 未找到星级/阶级图标文件: {iconFilePath}");
                    return;
                }
                int iconSpacing = 0;
                using (var levelIcon = Image.Load(iconFilePath))
                {
                    int iconWidth = levelIcon.Width;
                    int posY = 250;
                    if (isXyz)
                    {
                        // 阶级从左边开始
                        int leftStartX = 140;
                        for (int i = 0; i < card.Level.Value; i++)
                        {
                            int posX = leftStartX + (i * (iconWidth + iconSpacing));
                            image.Mutate(ctx => ctx.DrawImage(levelIcon, new Point(posX, posY), 1f));
                        }
                    }
                    else
                    {
                        // 星级从右边开始
                        int rightTopX = 1279;
                        for (int i = 0; i < card.Level.Value; i++)
                        {
                            int posX = rightTopX - (i * (iconWidth + iconSpacing));
                            image.Mutate(ctx => ctx.DrawImage(levelIcon, new Point(posX - iconWidth, posY), 1f));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"添加星级/阶级图标失败: {ex.Message}");
            }
        }
        // 拉丁字符
        private static bool IsLatinCharacter(char c)
        {
            if (c == '「' || c == '」' || c == '『' || c == '』' || c == '"' || c == '"' || c == '\'' || c == '\'')
                return false;
            // ASCII
            return c <= 127;
        }
        // 特殊分隔符
        private static bool IsSpecialSeparator(char c)
        {
            return c == '·' || c == '-' || c == '_' || c == '=' || c == '+' || c == '/';
        }
        // 判断是否为标点符号
        private static bool IsPunctuation(char c)
        {
            // 中文标点
            if (c == '，' || c == '。' || c == '、' || c == '：' || c == '；' ||
                c == '！' || c == '？' || c == '）' || c == '」' || c == '』')
                return true;
            // 英文标点
            if (c == ',' || c == '.' || c == ':' || c == ';' ||
                c == '!' || c == '?' || c == ')' || c == ']' || c == '}')
                return true;
            return false;
        }

        // 攻击力和守备力/Link值
        private static void AddAtkDefValues(Image image, Card card)
        {
            try
            {
                string atkDefFontPath = Path.Combine("asset", "font", "special", "ygo-atk-def.ttf");
                string linkFontPath = Path.Combine("asset", "font", "special", "ygo-link.ttf");
                if (!File.Exists(atkDefFontPath))
                {
                    Console.WriteLine($"错误: 未找到攻击力/守备力字体文件: {atkDefFontPath}");
                    return;
                }
                var fontCollection = new FontCollection();
                var atkDefFontFamily = fontCollection.Add(atkDefFontPath);
                var atkDefFont = atkDefFontFamily.CreateFont(60f, FontStyle.Bold);
                var color = Color.Black;
                string atkText = card.Atk ?? "?";
                if (atkText == "-1")
                {
                    atkText = "?";
                }
                if (atkText.Length < 4)
                {
                    atkText = atkText.PadLeft(4, ' ');
                }
                float atkX = 870f;
                float atkY = 1857f;
                image.Mutate(ctx => ctx.DrawText(atkText, atkDefFont, color, new PointF(atkX, atkY)));
                bool isLinkMonster = card.LinkValue.HasValue && card.LinkValue.Value > 0;
                if (isLinkMonster && card.LinkValue.HasValue)
                {
                    string linkText = card.LinkValue.Value.ToString();
                    float linkX = 1230f;
                    float linkY = 1890f;
                    if (File.Exists(linkFontPath))
                    {
                        var linkFontFamily = fontCollection.Add(linkFontPath);
                        var linkFont = linkFontFamily.CreateFont(50f, FontStyle.Bold);
                        image.Mutate(ctx => ctx.DrawText(linkText, linkFont, color, new PointF(linkX, linkY)));
                    }
                    else
                    {
                        Console.WriteLine($"警告: 未找到链接值字体文件: {linkFontPath},使用默认字体");
                        image.Mutate(ctx => ctx.DrawText(linkText, atkDefFont, color, new PointF(linkX, linkY)));
                    }
                }
                else if (!string.IsNullOrEmpty(card.Def))
                {
                    string defText = card.Def;
                    if (defText == "-1")
                    {
                        defText = "?";
                    }
                    if (defText.Length < 4)
                    {
                        defText = defText.PadLeft(4, ' ');
                    }
                    float defX = 1156f;
                    float defY = 1857f;
                    image.Mutate(ctx => ctx.DrawText(defText, atkDefFont, color, new PointF(defX, defY)));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"添加攻击力/守备力/链接值失败: {ex.Message}");
            }
        }
        // ID
        private static void AddCardID(Image image, Card card)
        {
            try
            {
                var frameType = card.FrameType?.ToLower() ?? "";
                string fontPath = Path.Combine("asset", "font", "special", "ygo-password.ttf");
                if (!File.Exists(fontPath))
                {
                    Console.WriteLine($"错误: 未找到ID字体文件: {fontPath}");
                    return;
                }
                var fontCollection = new FontCollection();
                var fontFamily = fontCollection.Add(fontPath);
                var font = fontFamily.CreateFont(50f, FontStyle.Regular);
                var color = frameType.Contains("xyz") ? Color.White : Color.Black;
                string idText = card.Id.ToString().PadLeft(8, '0');
                float idX = 64f;
                float idY = 1934f;
                image.Mutate(ctx => ctx.DrawText(idText, font, color, new PointF(idX, idY)));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"添加ID失败: {ex.Message}");
            }
        }
        // 卡图
        private static void AddCardImage(Image image, Card card, string figureDir)
        {
            try
            {
                string cardImagePath = Path.Combine(figureDir, $"{card.Id}.png");
                if (File.Exists(cardImagePath))
                {
                    using (var cardImage = Image.Load(cardImagePath))
                    {
                        var frameType = card.FrameType?.ToLower() ?? "";
                        int posX = frameType.Contains("pendulum") ? 94 : 171;
                        int posY = 376;
                        float scale = 1.7f;
                        var resizedImage = cardImage.Clone(ctx => ctx.Resize((int)(cardImage.Width * scale), (int)(cardImage.Height * scale)));
                        image.Mutate(ctx => ctx.DrawImage(resizedImage, new Point(posX, posY), 1f));
                    }
                }
                else
                {
                    Console.WriteLine($"警告: 未找到卡图: {cardImagePath},ID={card.Id}, 名称={card.Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"添加卡图失败: {ex.Message}, ID={card.Id}, 名称={card.Name}");
            }
        }
        // 魔法卡/陷阱卡字样及icon
        private static void AddCardTypeText(Image image, Card card)
        {
            try
            {
                if (card.CardType?.ToLower() != "spell" && card.CardType?.ToLower() != "trap")
                {
                    return;
                }
                bool isSpell = card.CardType?.ToLower() == "spell";
                string race = card.Race?.ToLower() ?? "normal";
                bool hasIcon = race != "normal";
                string fontPath = FontPath;
                if (!File.Exists(fontPath))
                {
                    Console.WriteLine($"错误: 未找到卡片类型字体文件: {fontPath}");
                    return;
                }
                var fontCollection = new FontCollection();
                var fontFamily = fontCollection.Add(fontPath);
                var font = fontFamily.CreateFont(100f, FontStyle.Regular);
                var color = Color.Black;
                float posY = 250f;
                float startX = hasIcon ? 750f : 840f;
                string prefixText = isSpell ? "【魔法卡" : "【陷阱卡";
                image.Mutate(ctx => ctx.DrawText(prefixText, font, color, new PointF(startX, posY)));
                var textOptions = new TextOptions(font)
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };
                FontRectangle prefixSize = TextMeasurer.MeasureSize(prefixText, textOptions);
                if (hasIcon)
                {
                    string iconName = $"icon-{race}.png";
                    string iconPath = Path.Combine("asset", "figure", IconsDir, iconName);
                    if (File.Exists(iconPath))
                    {
                        using (var iconImage = Image.Load(iconPath))
                        {
                            int iconX = 1160;
                            int iconY = 255;
                            float scale = 1.1f;
                            int newWidth = (int)(iconImage.Width * scale);
                            int newHeight = (int)(iconImage.Height * scale);
                            var resizedIcon = iconImage.Clone(ctx => ctx.Resize(newWidth, newHeight));
                            image.Mutate(ctx => ctx.DrawImage(resizedIcon, new Point(iconX, iconY), 1f));
                            // 后半部分 "】"
                            float suffixX = iconX + newWidth;
                            image.Mutate(ctx => ctx.DrawText("】", font, color, new PointF(suffixX, posY)));
                        }
                    }
                    else
                    {
                        Console.WriteLine($"警告: 未找到图标: {iconPath}");
                        image.Mutate(ctx => ctx.DrawText("】", font, color, new PointF(startX + prefixSize.Width, posY)));
                    }
                }
                else
                {
                    string fullText = isSpell ? "【魔法卡】" : "【陷阱卡】";
                    image.Mutate(ctx => ctx.DrawText(fullText, font, color, new PointF(840f, posY)));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"添加卡片类型文字失败: {ex.Message}");
            }
        }
        // Link箭头
        private static void AddLinkArrows(Image image, Card card, string assetFigureDir)
        {
            try
            {
                var frameType = card.FrameType?.ToLower() ?? "";
                if (frameType != "link" || card.LinkMarkers == null || card.LinkMarkers.Count == 0)
                {
                    return;
                }
                var allDirections = new List<string>
                {
                    "top-left", "top", "top-right",
                    "left", "right",
                    "bottom-left", "bottom", "bottom-right"
                };
                var arrowPositions = new Dictionary<string, Point>
                {
                    { "top-left", new Point(100, 300) },
                    { "top", new Point(570, 280) },
                    { "top-right", new Point(1140, 300) },
                    { "left", new Point(80, 760) },
                    { "right", new Point(1230, 760) },
                    { "bottom-left", new Point(100, 1335) },
                    { "bottom", new Point(570, 1427) },
                    { "bottom-right", new Point(1140, 1335) }
                };
                var cardLinkMarkers = card.LinkMarkers.Select(m => m.ToLower()).ToList();
                foreach (var direction in allDirections)
                {
                    bool isActive = cardLinkMarkers.Contains(direction);
                    string arrowFileName = $"arrow-{direction}-{(isActive ? "on" : "off")}.png";
                    string arrowFilePath = Path.Combine(assetFigureDir, ArrowsDir, arrowFileName);

                    if (File.Exists(arrowFilePath))
                    {
                        using (var arrowImage = Image.Load(arrowFilePath))
                        {
                            if (arrowPositions.TryGetValue(direction, out Point position))
                            {
                                image.Mutate(ctx => ctx.DrawImage(arrowImage, position, 1f));
                            }
                            else
                            {
                                Console.WriteLine($"警告: 未找到箭头位置配置: {direction}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"警告: 未找到箭头图片: {arrowFilePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"添加Link箭头标记失败: {ex.Message}");
            }
        }
        // 灵摆效果
        private static void DrawPendulumDescription(Image image, Card card)
        {
            try
            {
                var frameType = card.FrameType?.ToLower() ?? "";
                if (!frameType.Contains("pendulum") || string.IsNullOrEmpty(card.PendulumDescription))
                {
                    return;
                }
                float fontSize = 40f;
                var color = Color.Black;
                string pendulumText = card.PendulumDescription;
                string[] originalLines = pendulumText.Split('\n');
                float baseMaxEffectiveLength = 24f;
                int totalEffectiveLines = 0;
                foreach (string line in originalLines)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        totalEffectiveLines += 1;
                        continue;
                    }
                    float effectiveLength = CalculateEffectiveLength(line);
                    if (effectiveLength > baseMaxEffectiveLength)
                    {
                        List<string> subLines = SplitLongLine(line, baseMaxEffectiveLength);
                        totalEffectiveLines += subLines.Count;
                    }
                    else
                    {
                        totalEffectiveLines += 1;
                    }
                }
                float posX = PendulumDescriptionArea.X;
                float posY = PendulumDescriptionArea.Y;
                float maxWidth = PendulumDescriptionArea.Width;
                float maxHeight = PendulumDescriptionArea.Height;
                float lineHeight;
                float currentMaxEffectiveLength = baseMaxEffectiveLength;
                if (totalEffectiveLines > 5)
                {
                    fontSize = 33f;
                    lineHeight = fontSize * 1.1f;
                    currentMaxEffectiveLength = baseMaxEffectiveLength * (40f / 33f);
                }
                else if (totalEffectiveLines > 4)
                {
                    fontSize = 35f;
                    lineHeight = fontSize * 1.15f;
                    currentMaxEffectiveLength = baseMaxEffectiveLength * (40f / 35f);
                }
                else
                {
                    lineHeight = fontSize * 1.2f;
                }
                Font descFont = fontFamily.CreateFont(fontSize, FontStyle.Regular);
                var textOptions = new TextOptions(descFont)
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };
                int currentLine = 0;
                foreach (string line in originalLines)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        posY += lineHeight / 2;
                        currentLine++;
                        continue;
                    }
                    float effectiveLength = CalculateEffectiveLength(line);
                    if (effectiveLength > currentMaxEffectiveLength)
                    {
                        List<string> subLines = SplitLongLine(line, currentMaxEffectiveLength);
                        foreach (string subLine in subLines)
                        {
                            image.Mutate(ctx => ctx.DrawText(subLine, descFont, color, new PointF(posX, posY)));
                            posY += lineHeight;
                            currentLine++;
                        }
                    }
                    else
                    {
                        image.Mutate(ctx => ctx.DrawText(line, descFont, color, new PointF(posX, posY)));
                        posY += lineHeight;
                        currentLine++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"绘制灵摆效果描述失败: {ex.Message}");
            }
        }
        // 卡牌情报
        private static void DrawCardDescription(Image image, Card card)
        {
            try
            {
                if (string.IsNullOrEmpty(card.Description))
                {
                    return;
                }
                float fontSize = 40f;
                var color = Color.Black;
                string descriptionText = card.Description;
                bool isMonsterCard = card.CardType?.ToLower() == "monster";
                if (isMonsterCard && !string.IsNullOrEmpty(card.Typeline))
                {
                    descriptionText = card.Typeline + "\n" + descriptionText;
                }
                string[] originalLines = descriptionText.Split('\n');
                float baseMaxEffectiveLength = 29f;
                int newline_wraps = Math.Max(0, originalLines.Length - 1);
                int char_wraps = 0;
                int totalLinesByCalc = 0;
                foreach (string line in originalLines)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        totalLinesByCalc += 1;
                        continue;
                    }
                    float effectiveLength = CalculateEffectiveLength(line);
                    if (effectiveLength > baseMaxEffectiveLength)
                    {
                        List<string> subLines = SplitLongLine(line, baseMaxEffectiveLength);
                        totalLinesByCalc += subLines.Count;
                        char_wraps += Math.Max(0, subLines.Count - 1);
                    }
                    else
                    {
                        totalLinesByCalc += 1;
                    }
                }
                int totalLines = originalLines.Length + char_wraps;
                Console.WriteLine($"[Debug] ID={card.Id}, Name={card.Name}, newline_wraps={newline_wraps}, char_wraps={char_wraps}, totalLines={totalLines}");
                var frameType = card.FrameType?.ToLower() ?? "";
                bool isPendulum = frameType.Contains("pendulum");
                bool isSpellOrTrap = card.CardType?.ToLower() == "spell" || card.CardType?.ToLower() == "trap";
                float posX = CardDescriptionArea.X;
                float posY = CardDescriptionArea.Y;
                float maxWidth = CardDescriptionArea.Width;
                float maxHeight;
                if (isSpellOrTrap) {
                    maxHeight = 1897f - CardDescriptionArea.Y;
                } else if (isPendulum) {
                    posY = 1540f;
                    maxHeight = 1845f - 1540f;
                } else {
                    maxHeight = 1845f - CardDescriptionArea.Y;
                }
                float lineHeight;
                float currentMaxEffectiveLength = baseMaxEffectiveLength;
                if (newline_wraps >= 7)
                {
                    if (char_wraps >= 1) fontSize = 28f;
                    else fontSize = 32f;
                }
                else if (newline_wraps >= 5)
                {
                    if (char_wraps >= 3) fontSize = 30f;
                    else fontSize = 33f;
                }
                else if (newline_wraps >= 3)
                {
                    if (char_wraps >= 4) fontSize = 31f;
                    else fontSize = 33f;
                }
                else if (newline_wraps == 2)
                {
                    if (char_wraps >= 4) fontSize = 34f;
                    else fontSize = 38f;
                }
                else if (newline_wraps == 1)
                {
                    if (char_wraps >= 4) fontSize = 36f;
                    else fontSize = 40f;
                }
                else
                {
                    fontSize = 40f;
                }
                if (fontSize <= 30f)
                {
                    lineHeight = fontSize * 1.0f;
                }
                else if (fontSize <= 35f)
                {
                    lineHeight = fontSize * 1.1f;
                }
                else
                {
                    lineHeight = fontSize * 1.2f;
                }
                currentMaxEffectiveLength = baseMaxEffectiveLength * (40f / fontSize);
                Font descFont = fontFamily.CreateFont(fontSize, FontStyle.Regular);
                var textOptions = new TextOptions(descFont)
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };
                int currentLine = 0;
                foreach (string line in originalLines)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        posY += lineHeight / 2;
                        currentLine++;
                        continue;
                    }
                    float effectiveLength = CalculateEffectiveLength(line);
                    if (effectiveLength > currentMaxEffectiveLength)
                    {
                        List<string> subLines = SplitLongLine(line, currentMaxEffectiveLength);
                        foreach (string subLine in subLines)
                        {
                            image.Mutate(ctx => ctx.DrawText(subLine, descFont, color, new PointF(posX, posY)));
                            posY += lineHeight;
                            currentLine++;
                        }
                    }
                    else
                    {
                        image.Mutate(ctx => ctx.DrawText(line, descFont, color, new PointF(posX, posY)));
                        posY += lineHeight;
                        currentLine++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"绘制卡牌效果描述失败: {ex.Message}");
            }
        }
        // 分割长文本行
        private static List<string> SplitLongLine(string line, float maxEffectiveLength)
        {
            List<string> result = new List<string>();
            if (CalculateEffectiveLength(line) <= maxEffectiveLength)
            {
                result.Add(line);
                return result;
            }
            string currentLine = "";
            float currentLineLength = 0f;
            foreach (char c in line)
            {
                float charLength = 0f;
                if (IsLatinCharacter(c))
                {
                    charLength = 0.5f;
                }
                else if (IsSpecialSeparator(c))
                {
                    charLength = 0.7f;
                }
                else
                {
                    charLength = 1.0f;
                }
                if (currentLineLength + charLength > maxEffectiveLength)
                {
                    result.Add(currentLine);
                    currentLine = c.ToString();
                    currentLineLength = charLength;
                }
                else
                {
                    currentLine += c;
                    currentLineLength += charLength;
                }
            }
            if (!string.IsNullOrEmpty(currentLine))
            {
                result.Add(currentLine);
            }
            return result;
        }
    }
}
