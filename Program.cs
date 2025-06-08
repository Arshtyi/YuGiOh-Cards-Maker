using Yugioh;
class Program
{
    static void Main(string[] args)
    {
        var cardsJson = "tmp/cards.json";    // 卡片数据JSON文件
        var assetFigureDir = "asset/figure"; // 卡片框架资源目录
        var outputFigureDir = "figure";      // 输出目录
        CardGenerator.GenerateCards(cardsJson, assetFigureDir, outputFigureDir, 500);
    }
}
