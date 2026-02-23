using Yugioh;

class Program
{
    static void Main(string[] args)
    {
        var cardsJsonPath = "tmp/cards.json";
        var assetFigureDir = "asset/figure";
        var outputArtworkDir = "figure";
        bool debug = false;
        bool usePng = false;
        foreach (var arg in args)
        {
            if (arg.ToLower() == "--debug")
            {
                debug = true;
            }
            else if (arg.ToLower() == "--png")
            {
                usePng = true;
            }
        }
        if (debug)
        {
            AppLogger.Info("Program", "调试模式已启用，仅处理 dev/debug.txt 中指定 ID 的卡片。");
        }
        if (usePng)
        {
            AppLogger.Info("Program", "输出格式已设置为 PNG，将生成无损图像而非 JPG。");
        }
        CardGenerator.GenerateCardImages(cardsJsonPath, assetFigureDir, outputArtworkDir, debug, usePng);
    }
}
