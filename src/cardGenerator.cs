using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using System.Numerics;
using SixLabors.ImageSharp.PixelFormats;
namespace Yugioh
{
    public static class CardGenerator
    {
        private static readonly object failureFileLock = new object();
        private static readonly RectangleF CardNameArea = new RectangleF(70f, 96.10f, 1120.00f - 70f, 224.71f - 96.10f);
        private static readonly RectangleF PendulumDescriptionArea = new RectangleF(216f, 1286f, 1172f - 216f, 1498f - 1286f);
        private static readonly RectangleF CardDescriptionArea = new RectangleF(110f, 1533f, 1287f - 110f, 1897f - 1533f);
        private static readonly string FontPath = Path.Combine("asset", "font", "sc", "Yu-Gi-Oh! DFKaiW5-A（简体中文）.ttf");
        private static readonly string FramesDir = "cards";
        private static readonly string AttributesDir = "attributes";
        private static readonly string IndicatorsDir = "indicators";
        private static readonly string IconsDir = "icons";
        private static readonly string ArrowsDir = "arrows";
        private static readonly ConcurrentDictionary<string, Lazy<Image<Rgba32>>> imageCache = new();
        private static readonly ConcurrentDictionary<string, Image> resizedImageCache = new();
        private static Color titleBlackColor;
        private static Color titleWhiteColor;
        private static Color titleShadowColor;
        private static FontFamily fontFamily;
        private static FontFamily? atkDefFontFamily;
        private static FontFamily? linkFontFamily;
        private static FontFamily? passwordFontFamily;
        private static Image GetCachedImage(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"未找到图片文件: {filePath}");
            }
            var lazy = imageCache.GetOrAdd(filePath, p => new Lazy<Image<Rgba32>>(() => Image.Load<Rgba32>(p), LazyThreadSafetyMode.ExecutionAndPublication));
            return lazy.Value;
        }
        private static void ClearImageCache()
        {
            foreach (var kv in imageCache)
            {
                try
                {
                    if (kv.Value.IsValueCreated)
                    {
                        kv.Value.Value.Dispose();
                    }
                }
                catch { }
            }
            imageCache.Clear();

            foreach (var kv in resizedImageCache)
            {
                try
                {
                    kv.Value.Dispose();
                }
                catch { }
            }
            resizedImageCache.Clear();
        }
        private static FontCollection LoadFonts()
        {
            var fontCollection = new FontCollection();
            try
            {
                var fontFamily = fontCollection.Add(FontPath);
                CardGenerator.fontFamily = fontFamily;
                titleBlackColor = Color.Black;
                titleWhiteColor = Color.White;
                titleShadowColor = Color.FromRgba(0, 0, 0, 80);
                TryLoadSpecialFont(fontCollection, Path.Combine("asset", "font", "special", "Yu-Gi-Oh! Matrix（攻守刻度）.ttf"), out atkDefFontFamily);
                TryLoadSpecialFont(fontCollection, Path.Combine("asset", "font", "special", "Yu-Gi-Oh! RoGSanSrfStd-Bd（连接数）.ttf"), out linkFontFamily);
                TryLoadSpecialFont(fontCollection, Path.Combine("asset", "font", "special", "Yu-Gi-Oh! ITC Stone Serif M（编号卡密）.ttf"), out passwordFontFamily);
                return fontCollection;
            }
            catch (Exception ex)
            {
                AppLogger.Error("CardGenerator/Font", "字体加载失败。", ex);
                throw;
            }
        }
        private static void TryLoadSpecialFont(FontCollection collection, string path, out FontFamily? family)
        {
            family = null;
            try
            {
                if (File.Exists(path))
                {
                    family = collection.Add(path);
                }
                else
                {
                    AppLogger.Warn("CardGenerator/Font", $"未找到特殊字体文件：{path}");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warn("CardGenerator/Font", $"加载特殊字体失败：{path} | Exception={ex.Message}");
            }
        }
        public static void GenerateCardImages(string cardsJsonPath, string assetFigureDir, string outputFigureDir, bool debug = false, bool usePng = false)
        {
            AppLogger.Info("CardGenerator", "开始生成卡片图像。");
            if (!File.Exists(cardsJsonPath))
            {
                AppLogger.Error("CardGenerator", $"未找到卡片数据文件：{cardsJsonPath}");
                return;
            }
            try
            {
                string failureFile = Path.Combine("log", "failure.txt");
                var failureDir = Path.GetDirectoryName(failureFile);
                if (!string.IsNullOrEmpty(failureDir) && !Directory.Exists(failureDir))
                {
                    Directory.CreateDirectory(failureDir);
                }
                File.WriteAllText(failureFile, string.Empty);
            }
            catch (Exception ex)
            {
                AppLogger.Warn("CardGenerator", $"无法清空失败记录文件 log/failure.txt。Exception={ex.Message}");
            }
            // 清空输出
            if (Directory.Exists(outputFigureDir))
            {
                Directory.CreateDirectory(outputFigureDir);
                foreach (var file in Directory.GetFiles(outputFigureDir, "*.jpg").Concat(Directory.GetFiles(outputFigureDir, "*.png")))
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
                AppLogger.Error("CardGenerator", "卡片数据解析失败。");
                return;
            }
            var allValidCards = dict.Values.Where(c => !string.IsNullOrEmpty(c.FrameType)).ToList();
            List<Card> cardsToProcess;
            if (debug)
            {
                string debugFilePath = Path.Combine("dev", "debug.txt");
                if (File.Exists(debugFilePath))
                {
                    var debugIds = File.ReadAllLines(debugFilePath).Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => line.Trim()).ToList();
                    cardsToProcess = new List<Card>();
                    foreach (var idStr in debugIds)
                    {
                        if (dict.TryGetValue(idStr, out Card? card) && card != null && !string.IsNullOrEmpty(card.FrameType))
                        {
                            cardsToProcess.Add(card);
                        }
                        else
                        {
                            AppLogger.Warn("CardGenerator/Debug", $"debug.txt 中的 ID 在 cards.json 中不存在或无有效框架类型：{idStr}");
                        }
                    }
                    AppLogger.Info("CardGenerator/Debug", $"将仅处理 debug.txt 指定的卡片，共 {cardsToProcess.Count} 张。");
                    AppLogger.Info("CardGenerator/Debug", "调试模式下不会删除 tmp/figure 目录中的原始 PNG 文件。");
                    if (cardsToProcess.Count == 0)
                    {
                        AppLogger.Warn("CardGenerator/Debug", "debug.txt 中未匹配到任何有效 ID。");
                    }
                }
                else
                {
                    AppLogger.Warn("CardGenerator/Debug", $"未找到调试文件：{debugFilePath}，处理流程将退出。");
                    return;
                }
            }
            else
            {
                cardsToProcess = allValidCards;
                AppLogger.Info("CardGenerator", $"将处理全部有效卡片，共 {allValidCards.Count} 张。");
            }
            LoadFonts();
            // 并行处理
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            int processed = 0;
            int failed = 0;
            Parallel.ForEach(cardsToProcess, parallelOptions, card =>
            {
                try
                {
                    var frameType = card.FrameType?.ToLower() ?? "normal";
                    string? frameFile = null;
                    string exactFramePath = Path.Combine(assetFigureDir, FramesDir, $"card-{frameType}.png");
                    if (File.Exists(exactFramePath))
                    {
                        frameFile = exactFramePath;
                    }
                    if (frameFile == null)
                    {
                        string reason = $"无法找到卡框, 框架类型: {frameType}";
                        AppLogger.Error("CardGenerator", $"卡框加载失败。CardId={card.Id} CardName={card.Name} Reason={reason}");
                        WriteFailureRecord(card.Id, card.Name, reason);
                        Interlocked.Increment(ref failed);
                        return;
                    }
                    // 根据参数决定输出文件扩展名
                    string fileExtension = usePng ? ".png" : ".jpg";
                    var outPath = Path.Combine(outputFigureDir, $"{card.Id}{fileExtension}");
                    var frameImage = GetCachedImage(frameFile);

                    using (var image = new Image<Rgba32>(frameImage.Width, frameImage.Height))
                    {
                        // 卡图
                        bool hasArtwork = DrawCardArtwork(image, card, "tmp/figure");
                        if (!hasArtwork)
                        {
                            string reason = "加载中心图失败";
                            WriteFailureRecord(card.Id, card.Name, reason);
                            Interlocked.Increment(ref failed);
                            return;
                        }
                        image.Mutate(ctx => ctx.DrawImage(frameImage, new Point(0, 0), 1f));
                        // 属性
                        DrawAttributeImage(image, card, assetFigureDir);
                        // 攻守条
                        DrawAtkDefBar(image, card, assetFigureDir);
                        // 灵摆刻度
                        DrawPendulumScale(image, card);
                        // 星级/阶级图标
                        DrawLevelOrRank(image, card, assetFigureDir);
                        // Link箭头
                        DrawLinkArrows(image, card, assetFigureDir);
                        bool isXyzMonster = frameType.Contains("xyz");
                        bool isSpellOrTrap = card.CardType?.ToLower() == "spell" || card.CardType?.ToLower() == "trap";
                        bool isSpecialCard = isXyzMonster || isSpellOrTrap;
                        // 卡名
                        DrawCardName(image, card.Name, isSpecialCard);
                        // ID
                        DrawCardID(image, card);
                        // 魔法卡/陷阱卡类型文字
                        bool hasTypeIcon = DrawCardTypeText(image, card);
                        if (!hasTypeIcon)
                        {
                            string reason = "加载魔法/陷阱卡图标失败";
                            WriteFailureRecord(card.Id, card.Name, reason);
                            Interlocked.Increment(ref failed);
                            return;
                        }
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
                                    AppLogger.Warn("CardGenerator", $"无法删除临时 PNG：{tmpPngPath} | Exception={ex.Message}");
                                }
                            }
                        }
                    }
                    Interlocked.Increment(ref processed);
                    if (processed % 100 == 0)
                    {
                        AppLogger.Info("CardGenerator", $"处理进度：{processed}/{cardsToProcess.Count}。");
                    }
                }
                catch (Exception ex)
                {
                    string reason = $"处理异常: {ex.Message}";
                    AppLogger.Error("CardGenerator", $"卡片处理失败。CardId={card.Id} CardName={card.Name}", ex);
                    WriteFailureRecord(card.Id, card.Name, reason);
                    Interlocked.Increment(ref failed);
                }
            });
            ClearImageCache();
            AppLogger.Info("CardGenerator", $"卡片生成完成。成功={processed} 失败={failed}");
            int total = processed + failed;
            double successRate = 0.0;
            if (total > 0) successRate = (double)processed / total * 100.0;
            AppLogger.Info("CardGenerator", $"成功率={successRate:F2}% ({processed}/{total})");
            AppLogger.Info("CardGenerator", $"卡图输出目录：{Path.GetFullPath(outputFigureDir)}");
            string failureFilePath = Path.GetFullPath(Path.Combine("log", "failure.txt"));
            AppLogger.Info("CardGenerator", $"失败记录输出文件：{failureFilePath}");
        }
        // 卡名
        private static void DrawCardName(Image image, string cardName, bool isSpecialCard)
        {
            try
            {
                float fontSize = 95f; // 起始字号
                Color textColor = isSpecialCard ? titleWhiteColor : titleBlackColor;
                float areaWidth = CardNameArea.Width;
                float posX = CardNameArea.X + 20f; // 左边距
                int iterations = 0;
                float measuredWidth = 0f;
                Font titleFont;
                // 缩放循环：逐步减一直到不超出区域
                while (true)
                {
                    titleFont = fontFamily.CreateFont(fontSize, FontStyle.Regular);
                    // 先使用基于字符权重的估算快速判断是否可能超出
                    float baseMaxEffectiveLength = 25f;
                    float currentMaxEffectiveLength = baseMaxEffectiveLength * (95f / fontSize);
                    float effLen = ComputeEffectiveCharLength(cardName);
                    if (effLen <= currentMaxEffectiveLength)
                    {
                        measuredWidth = MeasureTextPixelWidth(cardName, titleFont);
                        if (measuredWidth <= areaWidth || fontSize <= 30f) break;
                    }
                    fontSize -= 0.5f;
                    iterations++;
                    if (iterations > 200)
                    {
                        AppLogger.Warn("CardGenerator/CardName", $"名称排版超过迭代上限，使用当前字体。Name={cardName}");
                        break;
                    }
                }
                float posYOffset = 20f + (95f - fontSize) * 0.4f; // 字号变小向下移动
                if (posYOffset < 15f) posYOffset = 15f;
                if (posYOffset > 60f) posYOffset = 60f;
                float posY = CardNameArea.Y + posYOffset;
                titleFont = fontFamily.CreateFont(fontSize, FontStyle.Regular);
                image.Mutate(ctx =>
                {
                    ctx.DrawText(cardName, titleFont, titleShadowColor, new PointF(posX + 3f, posY + 3f));
                    ctx.DrawText(cardName, titleFont, textColor, new PointF(posX, posY));
                });
                // float ratio = (areaWidth > 0f) ? (measuredWidth / areaWidth * 100f) : 0f;
                // Console.WriteLine($"[CardNameFit] 名称=\"{cardName}\" 像素长度={measuredWidth:F2}px 区域长度={areaWidth:F2}px 字体大小={fontSize:F1}px Y偏移={posYOffset:F1}px 占用比例={ratio:F1}% 迭代={iterations}");
            }
            catch (Exception ex)
            {
                AppLogger.Error("CardGenerator/CardName", "绘制卡名失败。", ex);
            }
        }
        // 返回单个字符的“有效长度”权重（供统一使用）
        private static float GetCharEffectiveLength(char c)
        {
            // 大写英文字母计为 0.7
            if (c >= 'A' && c <= 'Z')
            {
                return 0.6f;
            }
            // 小写英文字母计为 0.5
            if (c >= 'a' && c <= 'z')
            {
                return 0.5f;
            }
            if (c >= '0' && c <= '9')
            {
                return 0.5f;
            }
            if (c == '.' || c == '"' || c == '\'' || c == '，' || c == ' ')
            {
                return 0.5f;
            }
            if (c == '：') { return 0.6f; }
            if (c == '“' || c == '”' || c == '『' || c == '』' || c == '【' || c == '】' || c == '《' || c == '》' || c == '「' || c == '」' || c == '·')
            {
                return 1.0f;
            }
            if (c == '：' || c == '。') { return 0.7f; }
            // 中文汉字、日文假名等宽字符计为1个字符长度
            return 1.0f;
        }

        // 计算字符串的有效长度（按字符类型赋予不同权重）
        private static float ComputeEffectiveCharLength(string cardName)
        {
            float effectiveLength = 0f;
            if (string.IsNullOrEmpty(cardName)) return 0f;
            foreach (char c in cardName)
            {
                effectiveLength += GetCharEffectiveLength(c);
            }
            return effectiveLength;
        }
        // 使用 TextMeasurer 测量文本宽度
        private static float MeasureTextPixelWidth(string text, Font font)
        {
            try
            {
                if (string.IsNullOrEmpty(text)) return 0f;
                var options = new TextOptions(font);
                var rect = TextMeasurer.MeasureBounds(text, options);
                return rect.Width;
            }
            catch (Exception ex)
            {
                AppLogger.Warn("CardGenerator/CardName", $"文本宽度测量异常。Exception={ex.Message}");
                return 0f;
            }
        }
        // 绘制攻守条
        private static void DrawAtkDefBar(Image image, Card card, string assetFigureDir)
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
                        var atkDefImage = GetCachedImage(atkDefImagePath);

                        int posX = 106;
                        int posY = 1854;
                        // if (frameType.Contains("pendulum")) posY += 12;
                        image.Mutate(ctx => ctx.DrawImage(atkDefImage, new Point(posX, posY), 1f));

                        // 攻击力和守备力/Link值
                        DrawAtkDefValues(image, card);
                    }
                    else
                    {
                        AppLogger.Error("CardGenerator/AtkDefBar", $"未找到攻守条资源文件：{atkDefImagePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("CardGenerator/AtkDefBar", "绘制攻守条失败。", ex);
            }
        }
        // 属性
        private static void DrawAttributeImage(Image image, Card card, string assetFigureDir)
        {
            try
            {
                if (string.IsNullOrEmpty(card.AttributeName))
                {
                    return;
                }
                string attributeImageName = $"attribute-{card.AttributeName.ToLower()}.png";
                string attributeImagePath = Path.Combine(assetFigureDir, AttributesDir, attributeImageName);
                if (File.Exists(attributeImagePath))
                {
                    const float scale = 1.15f;
                    string cacheKey = $"{attributeImagePath}_{scale}";
                    var attributeImage = resizedImageCache.GetOrAdd(cacheKey, _ =>
                    {
                        var original = GetCachedImage(attributeImagePath);
                        var cloned = original.Clone(_ => { });
                        int scaledWidth = (int)Math.Round(original.Width * scale);
                        int scaledHeight = (int)Math.Round(original.Height * scale);
                        cloned.Mutate(ctx => ctx.Resize(new ResizeOptions { Size = new Size(scaledWidth, scaledHeight), Sampler = KnownResamplers.Lanczos3, Mode = ResizeMode.Stretch }));
                        return cloned;
                    });

                    int posX = 1152;
                    int posY = 86;
                    image.Mutate(ctx => ctx.DrawImage(attributeImage, new Point(posX, posY), 1f));
                }
                else
                {
                    AppLogger.Error("CardGenerator/Attribute", $"未找到属性图像资源：{attributeImagePath}");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("CardGenerator/Attribute", "绘制属性图像失败。", ex);
            }
        }
        // 灵摆刻度
        private static void DrawPendulumScale(Image image, Card card)
        {
            try
            {
                var frameType = card.FrameType?.ToLower() ?? "";
                if (!frameType.Contains("pendulum") || !card.PendulumScale.HasValue)
                {
                    return;
                }
                var pendulumFont = (atkDefFontFamily ?? fontFamily).CreateFont(90f, FontStyle.Bold);
                var color = Color.Black;
                int leftScaleX = 122;
                int ScaleY = 1400;
                int rightScaleX = 1226;
                string scaleText = card.PendulumScale.Value.ToString();
                int offsetX = 0;
                if (scaleText.Length > 1)
                {
                    offsetX = 26;
                }
                image.Mutate(ctx => ctx.DrawText(scaleText, pendulumFont, color, new PointF(leftScaleX - offsetX, ScaleY)));
                image.Mutate(ctx => ctx.DrawText(scaleText, pendulumFont, color, new PointF(rightScaleX - offsetX, ScaleY)));
            }
            catch (Exception ex)
            {
                AppLogger.Error("CardGenerator/PendulumScale", "绘制灵摆刻度失败。", ex);
            }
        }
        // 星级/阶级
        private static void DrawLevelOrRank(Image image, Card card, string assetFigureDir)
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
                    AppLogger.Error("CardGenerator/LevelRank", $"未找到星级/阶级图标资源：{iconFilePath}");
                    return;
                }
                int iconSpacing = 0;
                const float iconScale = 1.08f;
                string cacheKey = $"{iconFilePath}_{iconScale}";
                var levelIcon = resizedImageCache.GetOrAdd(cacheKey, _ =>
                {
                    var original = GetCachedImage(iconFilePath);
                    var cloned = original.Clone(_ => { });
                    if (iconScale != 1f)
                    {
                        int scaledWidth = Math.Max(1, (int)MathF.Round(original.Width * iconScale));
                        int scaledHeight = Math.Max(1, (int)MathF.Round(original.Height * iconScale));
                        cloned.Mutate(ctx => ctx.Resize(new ResizeOptions { Size = new Size(scaledWidth, scaledHeight), Sampler = KnownResamplers.Lanczos3, Mode = ResizeMode.Stretch }));
                    }
                    return cloned;
                });

                int iconWidth = levelIcon.Width;
                int posY = 245;
                if (isXyz)
                {
                    // 阶级从左边开始
                    // 特殊处理13阶
                    int leftStartX = card.Level.Value == 13 ? 80 : 123;
                    for (int i = 0; i < card.Level.Value; i++)
                    {
                        int posX = leftStartX + (i * (iconWidth + iconSpacing));
                        image.Mutate(ctx => ctx.DrawImage(levelIcon, new Point(posX, posY), 1f));
                    }
                }
                else
                {
                    // 星级从右边开始
                    // 特殊处理13星，尽管截止本更新时游戏王还没有卡面上的13星怪兽
                    int rightTopX = card.Level.Value == 13 ? 1328 : 1275;
                    for (int i = 0; i < card.Level.Value; i++)
                    {
                        int posX = rightTopX - (i * (iconWidth + iconSpacing));
                        image.Mutate(ctx => ctx.DrawImage(levelIcon, new Point(posX - iconWidth, posY), 1f));
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("CardGenerator/LevelRank", "绘制星级/阶级图标失败。", ex);
            }
        }
        // 攻击力和守备力/Link值
        private static void DrawAtkDefValues(Image image, Card card)
        {
            try
            {
                var atkDefFont = (atkDefFontFamily ?? fontFamily).CreateFont(60f, FontStyle.Bold);
                var color = Color.Black;
                string atkText = card.Attack ?? "?";
                if (atkText == "-1")
                {
                    atkText = "?";
                }
                if (atkText.Length < 4)
                {
                    atkText = atkText.PadLeft(4, ' ');
                }
                float atkX = 870f;
                float atkY = 1859f;
                // if (card.FrameType?.ToLower().Contains("pendulum") == true) atkY += 12f;
                image.Mutate(ctx => ctx.DrawText(atkText, atkDefFont, color, new PointF(atkX, atkY)));
                bool isLinkMonster = card.LinkValue.HasValue && card.LinkValue.Value > 0;
                if (isLinkMonster && card.LinkValue.HasValue)
                {
                    string linkText = card.LinkValue.Value.ToString();
                    float linkX = 1230f;
                    float linkY = 1890f;
                    // if (card.FrameType?.ToLower().Contains("pendulum") == true) linkY += 12f; // 暂时没有灵摆Link.真的会有吗？两种最具争议的召唤法的结合？
                    var linkFont = (linkFontFamily ?? fontFamily).CreateFont(50f, FontStyle.Bold);
                    image.Mutate(ctx => ctx.DrawText(linkText, linkFont, color, new PointF(linkX, linkY)));
                }
                else if (!string.IsNullOrEmpty(card.Defense))
                {
                    string defText = card.Defense;
                    if (defText == "-1")
                    {
                        defText = "?";
                    }
                    if (defText.Length < 4)
                    {
                        defText = defText.PadLeft(4, ' ');
                    }
                    float defX = 1156f;
                    float defY = 1859f;
                    // if (card.FrameType?.ToLower().Contains("pendulum") == true) defY += 12f;
                    image.Mutate(ctx => ctx.DrawText(defText, atkDefFont, color, new PointF(defX, defY)));
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("CardGenerator/AtkDefValue", "绘制攻击力/守备力/链接值失败。", ex);
            }
        }
        // ID
        private static void DrawCardID(Image image, Card card)
        {
            try
            {
                var frameType = card.FrameType?.ToLower() ?? "";
                var font = (passwordFontFamily ?? fontFamily).CreateFont(40f, FontStyle.Regular);
                var color = frameType.Contains("xyz") ? Color.White : Color.Black;
                string idText = card.Id.ToString().PadLeft(8, '0');
                float idX = 66f;
                float idY = frameType.Contains("pendulum") ? 1939f : 1935f;
                image.Mutate(ctx => ctx.DrawText(idText, font, color, new PointF(idX, idY)));
            }
            catch (Exception ex)
            {
                AppLogger.Error("CardGenerator/CardId", "绘制卡片 ID 失败。", ex);
            }
        }
        // 卡图
        private static bool DrawCardArtwork(Image image, Card card, string figureDir)
        {
            try
            {
                string cardImagePath = Path.Combine(figureDir, $"{card.Id}.png");
                if (File.Exists(cardImagePath))
                {
                    using (var cardImage = Image.Load(cardImagePath))
                    {
                        var frameType = card.FrameType?.ToLower() ?? "";
                        int posX = frameType.Contains("pendulum") ? 95 : 169;
                        int posY = frameType.Contains("pendulum") ? 365 : 376;
                        int targetWidth = 0;
                        int targetHeight = 0;
                        if (!frameType.Contains("pendulum"))
                        {
                            targetWidth = 1055;
                            targetHeight = 1053;
                        }
                        else
                        {
                            targetWidth = 1205;
                            if (cardImage.Width == 712 && cardImage.Height == 908)
                            {
                                targetHeight = 1546;
                            }
                            else if (cardImage.Width == 712 && cardImage.Height == 528)
                            {
                                targetHeight = 900;
                            }
                            else if (cardImage.Width == 710 && cardImage.Height == 530)
                            {
                                // Console.WriteLine($"警告: 灵摆卡图尺寸异常但已知，建议及时检查并修改。ID={card.Id}, 名称={card.Name}, 尺寸={cardImage.Width}x{cardImage.Height}");
                                targetHeight = 900;
                            }
                            else
                            {
                                AppLogger.Warn("CardGenerator/Artwork", $"灵摆卡图尺寸异常。CardId={card.Id} CardName={card.Name} Size={cardImage.Width}x{cardImage.Height}");
                            }
                        }
                        cardImage.Mutate(ctx => ctx.Resize(new ResizeOptions { Size = new Size(targetWidth, targetHeight), Sampler = KnownResamplers.Lanczos3, Mode = ResizeMode.Stretch }));
                        image.Mutate(ctx => ctx.DrawImage(cardImage, new Point(posX, posY), 1f));
                    }
                    return true;
                }
                else
                {
                    AppLogger.Warn("CardGenerator/Artwork", $"未找到卡图资源。CardId={card.Id} CardName={card.Name} Path={cardImagePath}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("CardGenerator/Artwork", $"绘制卡图失败。CardId={card.Id} CardName={card.Name}", ex);
                return false;
            }
        }
        // 魔法卡/陷阱卡字样及icon
        private static bool DrawCardTypeText(Image image, Card card)
        {
            try
            {
                if (card.CardType?.ToLower() != "spell" && card.CardType?.ToLower() != "trap")
                {
                    return true;
                }
                bool isSpell = card.CardType?.ToLower() == "spell";
                string race = card.Race?.ToLower() ?? "normal";
                bool hasIcon = race != "normal";
                var font = CardGenerator.fontFamily.CreateFont(80f, FontStyle.Regular);
                var color = Color.Black;
                float posY = 256f;
                if (hasIcon)
                {
                    float startX = hasIcon ? 790f : 880f;
                    string prefixText = isSpell ? "【魔法卡" : "【陷阱卡";
                    image.Mutate(ctx => ctx.DrawText(prefixText, font, color, new PointF(startX, posY)));
                    var textOptions = new TextOptions(font)
                    {
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    FontRectangle prefixSize = TextMeasurer.MeasureSize(prefixText, textOptions);
                    string iconName = $"icon-{race}.png";
                    string iconPath = Path.Combine("asset", "figure", IconsDir, iconName);
                    if (File.Exists(iconPath))
                    {
                        var iconImage = GetCachedImage(iconPath);

                        int iconX = 1120;
                        int iconY = 250;
                        float scale = 1.2f;
                        int newWidth = (int)(iconImage.Width * scale);
                        int newHeight = (int)(iconImage.Height * scale);
                        using (var resizedIcon = iconImage.Clone(ctx => ctx.Resize(new ResizeOptions { Size = new Size(newWidth, newHeight), Sampler = KnownResamplers.Lanczos3, Mode = ResizeMode.Stretch })))
                        {
                            image.Mutate(ctx => ctx.DrawImage(resizedIcon, new Point(iconX, iconY), 1f));
                        }
                        // 后半部分 "】"
                        float suffixX = iconX + newWidth;
                        image.Mutate(ctx => ctx.DrawText("】", font, color, new PointF(suffixX, posY)));

                    }
                    else
                    {
                        AppLogger.Warn("CardGenerator/CardType", $"未找到魔法/陷阱图标。CardId={card.Id} Path={iconPath}");
                        return false;
                    }
                }
                else
                {
                    string fullText = isSpell ? "【魔法卡】" : "【陷阱卡】";
                    image.Mutate(ctx => ctx.DrawText(fullText, font, color, new PointF(880f, posY)));
                }
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error("CardGenerator/CardType", "绘制卡片类型文本失败。", ex);
                return false;
            }
        }
        // Link箭头
        private static void DrawLinkArrows(Image image, Card card, string assetFigureDir)
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
                    { "top-left", new Point(118, 320) },
                    { "top", new Point(570, 299) },
                    { "top-right", new Point(1149, 320) },
                    { "left", new Point(94, 772) },
                    { "right", new Point(1224, 773) },
                    { "bottom-left", new Point(115, 1352) },
                    { "bottom", new Point(572, 1427) },
                    { "bottom-right", new Point(1148, 1351) }
                };
                var cardLinkMarkers = card.LinkMarkers.Select(m => m.ToLower()).ToList();
                foreach (var direction in allDirections)
                {
                    bool isActive = cardLinkMarkers.Contains(direction);
                    if (!isActive) continue;
                    string candidate = $"arrow-{direction}.png";
                    string candidatePath = Path.Combine(assetFigureDir, ArrowsDir, candidate);
                    if (File.Exists(candidatePath))
                    {
                        var arrowImage = GetCachedImage(candidatePath);

                        if (arrowPositions.TryGetValue(direction, out Point position))
                        {
                            image.Mutate(ctx => ctx.DrawImage(arrowImage, position, 1f));
                        }
                        else
                        {
                            AppLogger.Warn("CardGenerator/LinkArrow", $"未找到链接箭头位置配置：{direction}");
                        }

                    }
                    else
                    {
                        AppLogger.Warn("CardGenerator/LinkArrow", $"未找到链接箭头资源：{candidatePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("CardGenerator/LinkArrow", "绘制 Link 箭头失败。", ex);
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
                float fontSize = 40f; // 起始字号
                var color = Color.Black;
                string fullText = card.PendulumDescription;
                string[] logicalLines = fullText.Split('\n');
                float baseMaxEffectiveLength = 24f;
                float posX = PendulumDescriptionArea.X;
                float areaTopY = PendulumDescriptionArea.Y;
                float maxHeight = PendulumDescriptionArea.Height; // 高度限制
                int iterations = 0;
                int maxIterations = 60;
                List<string> finalLines = new();
                float finalLineHeight = 0f;
                float finalFontSize = fontSize;
                int finalTotalLines = 0;
                while (true)
                {
                    if (fontSize < 20f) fontSize = 20f;
                    float lineHeight;
                    if (fontSize <= 30f) lineHeight = fontSize * 1.0f; else if (fontSize <= 35f) lineHeight = fontSize * 1.1f; else lineHeight = fontSize * 1.2f;
                    float currentMaxEffectiveLength = baseMaxEffectiveLength * (40f / fontSize);
                    finalLines.Clear();
                    foreach (var l in logicalLines)
                    {
                        if (string.IsNullOrEmpty(l)) { finalLines.Add(""); continue; }
                        float eff = ComputeEffectiveCharLength(l);
                        if (eff > currentMaxEffectiveLength)
                        {
                            var subs = WrapLineByEffectiveLength(l, currentMaxEffectiveLength);
                            finalLines.AddRange(subs);
                        }
                        else finalLines.Add(l);
                    }
                    float totalHeight = 0f;
                    foreach (var ln in finalLines)
                        totalHeight += (ln == "") ? (lineHeight / 2f) : lineHeight;
                    if (totalHeight <= maxHeight || fontSize <= 20f || iterations >= maxIterations)
                    {
                        finalLineHeight = lineHeight;
                        finalFontSize = fontSize;
                        finalTotalLines = finalLines.Count;
                        break;
                    }
                    fontSize -= 0.5f;
                    iterations++;
                }
                var descFont = fontFamily.CreateFont(finalFontSize, FontStyle.Regular);
                float drawY = areaTopY;
                foreach (var line in finalLines)
                {
                    if (line == "") { drawY += finalLineHeight / 2f; continue; }
                    image.Mutate(ctx => ctx.DrawText(line, descFont, color, new PointF(posX, drawY)));
                    drawY += finalLineHeight;
                }
                // float usedHeight = drawY - areaTopY;
                // float ratio = (maxHeight > 0) ? usedHeight / maxHeight * 100f : 0f;
                // Console.WriteLine($"[PendulumDescFit] ID={card.Id} 名称=\"{card.Name}\" 字号={finalFontSize:F1}px 行数={finalTotalLines} 高度={usedHeight:F1}px/区域={maxHeight:F1}px 占用比例={ratio:F1}% 迭代={iterations}");
            }
            catch (Exception ex)
            {
                AppLogger.Error("CardGenerator/PendulumDescription", "绘制灵摆效果描述失败。", ex);
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
                if (isMonsterCard && !string.IsNullOrEmpty(card.TypeLine))
                {
                    descriptionText = card.TypeLine + "\n" + descriptionText;
                }
                var frameType = card.FrameType?.ToLower() ?? "";
                bool isPendulum = frameType.Contains("pendulum");
                bool isSpellOrTrap = card.CardType?.ToLower() == "spell" || card.CardType?.ToLower() == "trap";
                float baseAreaY = CardDescriptionArea.Y;
                float posX = CardDescriptionArea.X;
                float maxWidth = CardDescriptionArea.Width;
                float areaTopY = baseAreaY;
                float maxHeight;
                if (isSpellOrTrap)
                {
                    maxHeight = 1897f - baseAreaY;
                }
                else if (isPendulum)
                {
                    areaTopY = 1533f;
                    maxHeight = 1850f - 1533f;
                }
                else
                {
                    maxHeight = 1845f - baseAreaY;
                }
                float baseMaxEffectiveLength = 29.6f; // 对应字号40
                int iterations = 0;
                int maxIterations = 80;
                int finalTotalLines = 0;
                List<string> finalLines = new();
                float finalLineHeight = 0f;
                float finalFontSize = fontSize;
                // 迭代：如果排版高度超出区域则 fontSize-- 重排
                while (true)
                {
                    if (fontSize < 20f) { fontSize = 20f; }
                    float lineHeight;
                    if (fontSize <= 30f) lineHeight = fontSize * 1.0f;
                    else if (fontSize <= 35f) lineHeight = fontSize * 1.1f;
                    else lineHeight = fontSize * 1.2f;
                    float currentMaxEffectiveLength = baseMaxEffectiveLength * (40f / fontSize);
                    finalLines.Clear();
                    string[] logicalLines = descriptionText.Split('\n');
                    foreach (var l in logicalLines)
                    {
                        if (string.IsNullOrEmpty(l))
                        {
                            finalLines.Add("");
                            continue;
                        }
                        float eff = ComputeEffectiveCharLength(l);
                        if (eff > currentMaxEffectiveLength)
                        {
                            var subs = WrapLineByEffectiveLength(l, currentMaxEffectiveLength);
                            finalLines.AddRange(subs);
                        }
                        else
                        {
                            finalLines.Add(l);
                        }
                    }
                    int blankLines = finalLines.Count(fl => fl == "");
                    finalTotalLines = finalLines.Count;
                    float totalHeight = 0f;
                    foreach (var ln in finalLines)
                    {
                        if (ln == "") totalHeight += lineHeight / 2f; else totalHeight += lineHeight;
                    }
                    if (totalHeight <= maxHeight || fontSize <= 20f || iterations >= maxIterations)
                    {
                        finalLineHeight = lineHeight;
                        finalFontSize = fontSize;
                        break;
                    }
                    fontSize -= 0.5f;
                    iterations++;
                }
                var descFont = fontFamily.CreateFont(finalFontSize, FontStyle.Regular);
                // // 迭代结束后打印每一行的有效长度及该行的上限（基于最终字号），便于调试
                // try
                // {
                //     float finalMaxEffectiveLength = baseMaxEffectiveLength * (40f / finalFontSize);
                //     for (int i = 0; i < finalLines.Count; i++)
                //     {
                //         var ln = finalLines[i];
                //         float lnLen = string.IsNullOrEmpty(ln) ? 0f : ComputeEffectiveCharLength(ln);
                //         Console.WriteLine($"[CardDesc][ID={card.Id}] 行{i + 1}: 长度={lnLen:F2} 上限={finalMaxEffectiveLength:F2} 文本=\"{ln}\"");
                //     }
                // }
                // catch (Exception ex)
                // {
                //     Console.WriteLine($"[CardDesc] 打印行长度时异常: {ex.Message}");
                // }
                float drawY = areaTopY;
                foreach (var line in finalLines)
                {
                    if (line == "")
                    {
                        drawY += finalLineHeight / 2f;
                        continue;
                    }
                    image.Mutate(ctx => ctx.DrawText(line, descFont, color, new PointF(posX, drawY)));
                    drawY += finalLineHeight;
                }
                // float usedHeight = drawY - areaTopY;
                // float ratio = (maxHeight > 0f) ? usedHeight / maxHeight * 100f : 0f;
                // Console.WriteLine($"[CardDescFit] ID={card.Id} 名称=\"{card.Name}\" 字号={finalFontSize:F1}px 行数={finalTotalLines} 高度={usedHeight:F1}px/区域={maxHeight:F1}px 占用比例={ratio:F1}% 迭代={iterations}");
            }
            catch (Exception ex)
            {
                AppLogger.Error("CardGenerator/CardDescription", "绘制卡牌效果描述失败。", ex);
            }
        }
        private static List<string> WrapLineByEffectiveLength(string line, float maxEffectiveLength)
        {
            List<string> result = new List<string>();
            if (ComputeEffectiveCharLength(line) <= maxEffectiveLength)
            {
                result.Add(line);
                return result;
            }
            string currentLine = "";
            float currentLineLength = 0f;
            foreach (char c in line)
            {
                float charLength = GetCharEffectiveLength(c);
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
        private static void WriteFailureRecord(object idObj, string name, string reason)
        {
            try
            {
                string id = idObj?.ToString() ?? "";
                string failureFile = Path.Combine("log", "failure.txt");
                var dir = Path.GetDirectoryName(failureFile);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [WARN] [CardGenerator/FailureRecord] CardId={id} CardName={name} Reason={reason}{Environment.NewLine}";
                lock (failureFileLock)
                {
                    File.AppendAllText(failureFile, line);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warn("CardGenerator/FailureRecord", $"无法写入失败记录文件 log/failure.txt。Exception={ex.Message}");
            }
        }
    }
}
