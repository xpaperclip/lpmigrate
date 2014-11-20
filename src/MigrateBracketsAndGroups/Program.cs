using System;
using System.IO;
using System.Windows.Forms;
using LxTools;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.Run(new MainForm());
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

            string wikicode = MigrateCore.Get(page);

            var bracket = MigrateCore.AnalyzeAndMigrate(wikicode);
            UI.ShowDialog(new UIDocument("Bracket", bracket));

            Console.WriteLine();
        //}

        //Console.ReadLine();
    }

}
