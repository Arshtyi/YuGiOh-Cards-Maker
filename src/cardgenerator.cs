using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using System.Numerics;
namespace Yugioh
{
    public static class CardGenerator
    {
        private static readonly RectangleF CardNameArea = new RectangleF(89.86f, 96.10f, 1113.00f - 89.86f, 224.71f - 96.10f);
        private static readonly string FontPath = Path.Combine("asset", "font", "sc", "XinHuaKaiTi.ttf");
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

        public static void GenerateCards(string cardsJsonPath, string assetFigureDir, string outputFigureDir, int maxCount = int.MaxValue)
        {
            Console.WriteLine("开始卡片图像生成...");
            if (!File.Exists(cardsJsonPath))
            {
                Console.WriteLine($"错误: 未找到卡片数据文件: {cardsJsonPath}");
                return;
            }
            // 清空输出目录
            if (Directory.Exists(outputFigureDir))
            {
                Directory.CreateDirectory(outputFigureDir);
                foreach (var file in Directory.GetFiles(outputFigureDir, "*.png"))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // 忽略删除错误
                    }
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
            
            if (maxCount < allValidCards.Count)
            {
                cardsToProcess = allValidCards.Take(maxCount).ToList();
                Console.WriteLine($"将处理前{maxCount}张卡片");
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
                    
                    string exactFramePath = Path.Combine(assetFigureDir, $"card-{frameType}.png");
                    if (File.Exists(exactFramePath))
                    {
                        frameFile = exactFramePath;
                    }
                    if (frameFile == null)
                    {
                        Console.WriteLine($"错误: 无法找到卡框文件，卡片ID: {card.Id}, 名称: {card.Name}, 框架类型: {frameType}");
                        Interlocked.Increment(ref failed);
                        return;
                    }
                    var outPath = Path.Combine(outputFigureDir, $"{card.Id}.png");
                    using (var image = Image.Load(frameFile))
                    {
                        // 添加属性图
                        AddAttributeImage(image, card, assetFigureDir);
                        // 添加卡图
                        AddCardImage(image, card, "tmp/figure");
                        // 如果是灵摆卡片，覆盖灵摆框
                        if (frameType.Contains("pendulum"))
                        {
                            string pendulumMaskPath = Path.Combine(assetFigureDir, "card-mask-pendulum.png");
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
                        // 添加攻守条
                        AddAtkDefImage(image, card, assetFigureDir);
                        // 添加灵摆刻度
                        AddPendulumScale(image, card);
                        // 添加星级/阶级图标
                        AddLevelOrRank(image, card, assetFigureDir);
                        bool isXyzMonster = frameType.Contains("xyz");
                        bool isSpellOrTrap = card.CardType?.ToLower() == "spell" || card.CardType?.ToLower() == "trap";
                        bool isSpecialCard = isXyzMonster || isSpellOrTrap;
                        DrawCardName(image, card.Name, isSpecialCard);
                        // 添加卡片ID
                        AddCardID(image, card);
                        image.Save(outPath);
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
        
        // 绘制卡名 ，使用动态字体大小适配长卡名
        private static void DrawCardName(Image image, string cardName, bool isSpecialCard)
        {
            try
            {
                float fontSize = 95f;
                float posYOffset = 30f;
                bool hasSpecialSeparator = cardName.Contains("·") || cardName.Contains("-") || cardName.Contains("・");
                float effectiveLength = CalculateEffectiveLength(cardName);
                if (effectiveLength <= 12)
                {
                    fontSize = 105f;
                    posYOffset = 25f;
                }
                else if (effectiveLength <= 20)
                {
                    fontSize = 62f;
                    posYOffset = 41f;
                }
                else if (effectiveLength <= 24)
                {
                    fontSize = 58f;
                    posYOffset = 43f;
                }
                else
                {
                    fontSize = 54f;
                    posYOffset = 45f;
                }
                Font nameFont = fontFamily.CreateFont(fontSize, FontStyle.Regular);
                Color textColor = isSpecialCard ? nameWhiteColor : nameBlackColor;
                var textOptions = new TextOptions(nameFont)
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };
                
                FontRectangle size = TextMeasurer.MeasureSize(cardName, textOptions);
                float width = size.Width;
                float maxWidth = CardNameArea.Width * 0.97f;
                float sx = 1.0f;
                if (width > maxWidth)
                {
                    sx = maxWidth / width;
                    float minScale = 0.96f;
                    if (effectiveLength <= 1.5f || (hasSpecialSeparator && effectiveLength <= 10f))
                    {
                        minScale = 0.98f; 
                    }
                    else if (effectiveLength > 20f)
                    {
                        minScale = 0.75f;
                    }
                    else if (effectiveLength > 16f)
                    {
                        minScale = 0.85f;
                    }
                    else if (effectiveLength > 12f)
                    {
                        minScale = 0.89f;
                    }
                    else if (effectiveLength > 10f)
                    {
                        minScale = 0.92f;
                    }
                    else if (effectiveLength > 8f)
                    {
                        minScale = 0.93f;
                    }
                    else if (effectiveLength > 6f)
                    {
                        minScale = 0.95f; 
                    }
                    else if (effectiveLength > 4f)
                    {
                        minScale = 0.96f; 
                    }
                    
                    if (sx < minScale)
                    {
                        sx = minScale;
                        int attempts = 0;
                        float reductionFactor = 0.95f;
                        if (effectiveLength <= 1.5f || hasSpecialSeparator)
                        {
                            reductionFactor = 0.98f;
                        }
                        while (width * sx > maxWidth * 1.02f && attempts < 3)
                        {
                            fontSize = fontSize * reductionFactor;
                            nameFont = fontFamily.CreateFont(fontSize, FontStyle.Regular);
                            textOptions = new TextOptions(nameFont)
                            {
                                HorizontalAlignment = HorizontalAlignment.Left,
                                VerticalAlignment = VerticalAlignment.Top
                            };
                            size = TextMeasurer.MeasureSize(cardName, textOptions);
                            width = size.Width;
                            if (width > maxWidth)
                            {
                                sx = Math.Max(maxWidth / width, minScale);
                            }
                            else
                            {
                                sx = 1.0f;
                                break;
                            }
                            attempts++;
                        }
                    }
                }
                float posX = CardNameArea.X + 20f;
                float posY = CardNameArea.Y + posYOffset;
                image.Mutate(ctx =>
                {
                    Matrix3x2 matrix = Matrix3x2.CreateScale(sx, 1.0f) * Matrix3x2.CreateTranslation(posX, posY);
                    ctx.SetDrawingTransform(matrix);
                    ctx.DrawText(cardName, nameFont, nameShadowColor, new PointF(3f, 3f));
                    ctx.DrawText(cardName, nameFont, textColor, new PointF(0f, 0f));
                    ctx.SetDrawingTransform(Matrix3x2.Identity);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"绘制卡名失败: {ex.Message}");
            }
        }
        // 计算卡名的有效长度
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
                    string atkDefImagePath = Path.Combine(assetFigureDir, atkDefImageName);
                    if (File.Exists(atkDefImagePath))
                    {
                        using (var atkDefImage = Image.Load(atkDefImagePath))
                        {
                            int posX = 106;
                            int posY = 1854; 
                            image.Mutate(ctx => ctx.DrawImage(atkDefImage, new Point(posX, posY), 1f));
                        }
                        // 添加攻击力和守备力（包括LINK值）
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
        
        // 添加属性图
        private static void AddAttributeImage(Image image, Card card, string assetFigureDir)
        {
            try
            {
                if (string.IsNullOrEmpty(card.Attribute))
                {
                    return;
                }
                string attributeImageName = $"attribute-{card.Attribute.ToLower()}.png";
                string attributeImagePath = Path.Combine(assetFigureDir, attributeImageName);
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
        
        // 添加灵摆刻度
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
        
        // 添加星级/阶级图标
        private static void AddLevelOrRank(Image image, Card card, string assetFigureDir)
        {
            try
            {
                var frameType = card.FrameType?.ToLower() ?? "";
                bool isMonsterCard = card.CardType?.ToLower() == "monster";
                if (!isMonsterCard || !card.Level.HasValue || card.Level.Value <= 0)
                {
                    return;
                }
                bool isXyz = frameType.Contains("xyz");
                string iconFileName = isXyz ? "rank.png" : "level.png";
                string iconFilePath = Path.Combine(assetFigureDir, iconFileName);
                if (!File.Exists(iconFilePath))
                {
                    Console.WriteLine($"错误: 未找到星级/阶级图标文件: {iconFilePath}");
                    return;
                }
                // 最右边一个星级图标的右上角坐标
                int rightTopX = 1279;
                int rightTopY = 271;
                int iconSpacing = 0; 
                
                using (var levelIcon = Image.Load(iconFilePath))
                {
                    int iconWidth = levelIcon.Width;
                    for (int i = 0; i < card.Level.Value; i++)
                    {
                        int posX = rightTopX - (i * (iconWidth + iconSpacing));
                        image.Mutate(ctx => ctx.DrawImage(levelIcon, new Point(posX - iconWidth, rightTopY), 1f));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"添加星级/阶级图标失败: {ex.Message}");
            }
        }
        
        // 判断是否为拉丁字符（英文字母、数字、常见标点等）
        private static bool IsLatinCharacter(char c)
        {
            // ASCII 码范围（基本拉丁字符集）
            return c <= 127;
        }
        // 判断是否为特殊分隔符
        private static bool IsSpecialSeparator(char c)
        {
            return c == '·' || c == '-' || c == '・' || c == '_' || c == '=' || c == '+' || c == '/';
        }
        
        // 添加攻击力和守备力/链接值数值
        private static void AddAtkDefValues(Image image, Card card)
        {
            try
            {
                string fontPath = Path.Combine("asset", "font", "special", "ygo-atk-def.ttf");
                if (!File.Exists(fontPath))
                {
                    Console.WriteLine($"错误: 未找到攻击力/守备力字体文件: {fontPath}");
                    return;
                }
                var fontCollection = new FontCollection();
                var fontFamily = fontCollection.Add(fontPath);
                var font = fontFamily.CreateFont(60f, FontStyle.Bold);
                var color = Color.Black;
                string atkText = card.Atk;
                if (atkText == "-1")
                {
                    atkText = "?";
                }
                float atkX = 870f;
                float atkY = 1857f;
                image.Mutate(ctx => ctx.DrawText(atkText, font, color, new PointF(atkX, atkY)));
                bool isLinkMonster = card.LinkValue.HasValue && card.LinkValue.Value > 0;
                if (isLinkMonster) 
                {
                    string linkText = card.LinkValue.Value.ToString();
                    float linkX = 1235f;
                    float linkY = 1857f;
                    image.Mutate(ctx => ctx.DrawText(linkText, font, color, new PointF(linkX, linkY)));
                }
                else if (!string.IsNullOrEmpty(card.Def))
                {
                    string defText = card.Def;
                    if (defText == "-1")
                    {
                        defText = "?";
                    }
                    float defX = 1156f;
                    float defY = 1857f;
                    image.Mutate(ctx => ctx.DrawText(defText, font, color, new PointF(defX, defY)));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"添加攻击力/守备力/链接值数值失败: {ex.Message}");
            }
        }
        // 添加卡片ID
        private static void AddCardID(Image image, Card card)
        {
            try
            {
                var frameType = card.FrameType?.ToLower() ?? "";
                string fontPath = Path.Combine("asset", "font", "special", "ygo-password.ttf");
                if (!File.Exists(fontPath))
                {
                    Console.WriteLine($"错误: 未找到卡片ID字体文件: {fontPath}");
                    return;
                }
                
                var fontCollection = new FontCollection();
                var fontFamily = fontCollection.Add(fontPath);
                var font = fontFamily.CreateFont(50f, FontStyle.Regular); 
                // 对于XYZ卡使用白色，其他卡使用黑色
                var color = frameType.Contains("xyz") ? Color.White : Color.Black;
                // 将ID格式化为8位，不足的前面补0
                string idText = card.Id.ToString().PadLeft(8, '0');
                float idX = 64f;
                float idY = 1934f;
                image.Mutate(ctx => ctx.DrawText(idText, font, color, new PointF(idX, idY)));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"添加卡片ID失败: {ex.Message}");
            }
        }
        
        // 添加卡图
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
                    Console.WriteLine($"警告: 未找到卡图: {cardImagePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"添加卡图失败: {ex.Message}");
            }
        }
    }
}
