using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;

namespace Yugioh
{
    public static class CardGenerator
    {
        public static void GenerateCards(string cardsJsonPath, string assetFigureDir, string outputFigureDir, int maxCount = 500)
        {
            Console.WriteLine("开始卡片图像生成...");
            
            if (!File.Exists(cardsJsonPath))
            {
                Console.WriteLine($"错误: 未找到卡片数据文件: {cardsJsonPath}");
                return;
            }
            
            // 确保输出目录存在
            Directory.CreateDirectory(outputFigureDir);
            
            Console.WriteLine("正在加载卡片数据...");
            var json = File.ReadAllText(cardsJsonPath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, Card>>(json);
            if (dict == null)
            {
                Console.WriteLine("错误: 卡片数据解析失败");
                return;
            }
            
            // 随机选择卡片
            Console.WriteLine($"从{dict.Count}张卡片中随机选择{maxCount}张进行处理...");
            var rnd = new Random();
            var cards = dict.Values.Where(c => !string.IsNullOrEmpty(c.FrameType)).OrderBy(_ => rnd.Next()).Take(maxCount).ToList();
            
            // 并行处理每张卡
            var options = new ParallelOptions { MaxDegreeOfParallelism = 500 };
            int processed = 0;
            int failed = 0;
            
            Console.WriteLine($"开始并行处理{cards.Count}张卡片，最大并行度: {options.MaxDegreeOfParallelism}");
            
            Parallel.ForEach(cards, options, card =>
            {
                try
                {
                    var frameType = card.FrameType?.ToLower() ?? "normal";
                    string frameFile = null;
                    
                    // 直接根据frameType构造卡框文件路径
                    string exactFramePath = Path.Combine(assetFigureDir, $"card-{frameType}.png");
                    if (File.Exists(exactFramePath))
                    {
                        frameFile = exactFramePath;
                    }
                    
                    // 如果未找到，使用通用normal卡框
                    if (frameFile == null || !File.Exists(frameFile))
                    {
                        frameFile = Path.Combine(assetFigureDir, "card-normal.png");
                        if (!File.Exists(frameFile))
                        {
                            // 最后尝试使用任何可用的卡框
                            var anyFrame = Directory.GetFiles(assetFigureDir, "card-*.png").FirstOrDefault();
                            if (anyFrame != null)
                            {
                                frameFile = anyFrame;
                            }
                        }
                    }
                    
                    if (frameFile == null)
                    {
                        Interlocked.Increment(ref failed);
                        Console.WriteLine($"未找到frameType={frameType}的卡框");
                        return;
                    }
                    
                    var outPath = Path.Combine(outputFigureDir, $"{card.Id}.png");
                    File.Copy(frameFile, outPath, true);
                    
                    Interlocked.Increment(ref processed);
                    if (processed % 50 == 0)
                    {
                        Console.WriteLine($"已处理: {processed}/{cards.Count} 张卡片");
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failed);
                    Console.WriteLine($"生成卡片{card.Id}时出错: {ex.Message}");
                }
            });
            
            Console.WriteLine($"卡片生成完成！成功: {processed}, 失败: {failed}");
            Console.WriteLine($"输出目录: {Path.GetFullPath(outputFigureDir)}");
        }
    }
}
