using System;
using System.IO;
using System.Windows.Forms;
using LxTools;
using LxTools.Liquipedia;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.Run(new MainForm());
    }

    public static string Get(string page)
    {
        Directory.CreateDirectory("cache");
        string local = Path.Combine("cache", page.Replace(" ", "_").Replace("/", "!"));
        if (File.Exists(local))
            return File.ReadAllText(local);

        string url = "http://wiki.teamliquid.net/starcraft/" + page;
        string wikicode = LiquipediaClient.GetWikicode(url);
        File.WriteAllText(local, wikicode);

        return wikicode;
    }

    static void MainX(string[] args)
    {
        //var list = File.ReadAllLines("MatchSummary.list");
        //foreach (string page in list)
        //{
            string page = "2008 Arena MSL";

            //if (string.IsNullOrWhiteSpace(page)) continue;
            //if (page.StartsWith(";")) continue;

            ConsoleEx.WriteLine(ConsoleColor.White, ConsoleColor.Blue, page);

            string wikicode = Get(page);

            var bracket = MigrateCore.AnalyzeAndMigrateBrackets(wikicode);
            UI.ShowDialog(new UIDocument("Bracket", bracket));

            Console.WriteLine();
        //}

        //Console.ReadLine();
    }

}
