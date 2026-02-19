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
            Console.WriteLine("启用调试模式: 仅处理dev/debug.txt中指定ID的卡片");
        }
        if (usePng)
        {
            Console.WriteLine("使用PNG格式: 将生成无损PNG图像而非JPG图像");
        }
        CardGenerator.GenerateCardImages(cardsJsonPath, assetFigureDir, outputArtworkDir, debug, usePng);
    }
}
