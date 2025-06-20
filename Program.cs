using Yugioh;

class Program
{
    static void Main(string[] args)
    {
        var cardsJson = "tmp/cards.json";   
        var assetFigureDir = "asset/figure"; 
        var outputFigureDir = "figure"; 
        bool debug = false;
        if (args.Length > 0 && args[0].ToLower() == "debug")
        {
            debug = true;
            Console.WriteLine("启用调试模式: 仅处理dev/debug.txt中指定ID的卡片");
        }
        CardGenerator.GenerateCards(cardsJson, assetFigureDir, outputFigureDir, debug);
    }
}
