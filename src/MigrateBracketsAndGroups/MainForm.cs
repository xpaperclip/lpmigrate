using System;
using System.Windows.Forms;
using LxTools;
using LxTools.Liquipedia;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }

    private void MainForm_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.Text))
        {
            string text = e.Data.GetData(DataFormats.Text).ToString();
            if (LiquipediaClient.IsValidLiquipediaLink(text))
                e.Effect = DragDropEffects.Link;
            else
                e.Effect = DragDropEffects.None;
        }
        else
        {
            e.Effect = DragDropEffects.None;
        }
    }
    private void MainForm_DragDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.Text))
        {
            string text = e.Data.GetData(DataFormats.Text).ToString();
            if (LiquipediaClient.IsValidLiquipediaLink(text))
            {
                var thread = new System.Threading.Thread(() =>
                {
                    string wikicode = LiquipediaClient.GetWikicode(text);
                    var bracket = MigrateCore.AnalyzeAndMigrate(wikicode);
                    UI.ShowDialog(new UIDocument("Migrated", bracket));
                });
                thread.Start();
            }
            else
            {
                MessageBox.Show("Did not recognise Liquipedia text.");
            }
        }
    }
}
