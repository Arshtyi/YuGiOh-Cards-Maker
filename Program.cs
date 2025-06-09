using Yugioh;

class Program
{
    static void Main(string[] args)
    {
        var cardsJson = "tmp/cards.json";   
        var assetFigureDir = "asset/figure"; 
        var outputFigureDir = "figure"; 
        CardGenerator.GenerateCards(cardsJson, assetFigureDir, outputFigureDir, 100);
    }
}
