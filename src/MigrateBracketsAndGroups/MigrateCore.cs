using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LxTools;
using LxTools.Liquipedia;
using LxTools.Liquipedia.Parsing2;
using System.Windows.Forms;

struct BracketInfo
{
    public string Name;
    public Dictionary<string, MatchId> Matches;
    public WikiTemplateNode Node;
}
class MatchId
{
    public string P1Var;
    public string P2Var;
    public string MatchVar;
    public BracketLine P1;
    public BracketLine P2;
    public MatchInfo MatchInfo;
}
struct BracketLine
{
    public string Id;
    public string Flag;
    public string Race;
    public string Score;
    public string Link;
    public bool Win;
}
struct MatchInfo
{
    public BracketLine P1;
    public BracketLine P2;
    public WikiTemplateNode Node;
    public string Section;
    public string LastText;
}

public static class MigrateCore
{
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

    public static string AnalyzeAndMigrate(string wikicode)
    {
        var wiki = WikiParser.Parse(wikicode, true);

        var interested = new string[] { "Match", "MatchSummary", "GameSet", "Vod", "Vodlink" };
        var bracket = new BracketInfo();
        var matches = new List<MatchInfo>();
        var matchesLookup = new Dictionary<string, MatchInfo>();
        var section = "";
        var lasttext = "";

        foreach (var n in wiki)
        {
            if (n is WikiTextNode)
            {
                var node = n as WikiTextNode;
                if (node.Type == WikiTextNodeType.Section2) ConsoleEx.WriteLine(ConsoleColor.Green, "==" + node.Text + "==");
                if (node.Type == WikiTextNodeType.Section3) ConsoleEx.WriteLine(ConsoleColor.Yellow, "===" + node.Text + "===");
                if (node.Type == WikiTextNodeType.Section4) ConsoleEx.WriteLine(ConsoleColor.DarkYellow, "====" + node.Text + "====");
                if (node.Type == WikiTextNodeType.Section5) ConsoleEx.WriteLine(ConsoleColor.DarkYellow, "=====" + node.Text + "=====");
                if (node.IsSection()) section = node.Text;
                if (node.Type == WikiTextNodeType.Text)
                {
                    lasttext = node.Text;
                    ConsoleEx.WriteLine(ConsoleColor.Gray, node.Text);
                }
            }
            if (n is WikiTemplateNode)
            {
                var node = n as WikiTemplateNode;
                if (node.Name.EndsWith("Bracket") || interested.Contains(node.Name))
                {
                    Console.WriteLine("{{" + node.Name + "}}");
                    if (node.Name.EndsWith("Bracket"))
                    {
                        var analyzedBracket = AnalyzeBracket(node);
                        if (analyzedBracket.Name == null) continue;
                        if (bracket.Name != null)
                        {
                            ConsoleEx.WriteLine(ConsoleColor.Red, "Already found a bracket on this page.");
                            Console.ReadLine();
                        }
                        bracket = analyzedBracket;
                    }
                    if (node.Name == "Match" || node.Name == "MatchSummary")
                    {
                        var analyzedMatch = AnalyzeMatch(node);
                        analyzedMatch.Section = section;
                        analyzedMatch.LastText = lasttext;
                        matches.Add(analyzedMatch);

                        matchesLookup.Add(GetLookup(analyzedMatch.P1, analyzedMatch.P2), analyzedMatch);
                    }
                }
            }
        }

        if (bracket.Name == null)
            return "No recognised brackets.";

        // combine found matches
        foreach (var match in bracket.Matches.Values)
        {
            MatchInfo result;
            var key = GetLookup(match.P1, match.P2);
            var key2 = GetLookup(match.P2, match.P1);
            if (matchesLookup.TryGetValue(key, out result))
            {
                match.MatchInfo = result;
            }
            else if (matchesLookup.TryGetValue(key2, out result))
            {
                match.MatchInfo = result;
            }
            else
            {
                ConsoleEx.WriteLine(ConsoleColor.Red, "Could not find match information: {0}", key);
                //Console.ReadLine();
            }
        }

        return WriteBracket(bracket);
    }

    private static string WriteBracket(BracketInfo bracket)
    {
        string fmtfile = Path.Combine("LPfmt", bracket.Name + ".bracketfmt");
        if (!File.Exists(fmtfile))
        {
            ConsoleEx.WriteLine(ConsoleColor.Red, "Bracket not recognised");
            Console.ReadLine();
        }

        using (var sw = new StringWriter())
        using (var fmtsr = new StreamReader(fmtfile))
        {
            sw.WriteLine("{{" + bracket.Name);

            // check if |Rx exist
            for (int i = 1; i <= 7; i++)
            {
                var Rtext = bracket.Node.GetParamText("R" + i.ToString());
                if (Rtext != null)
                {
                    sw.WriteLine("|R{0}={1}", i, Rtext);
                }
            }
            sw.WriteLine();

            string fmtstring;
            while ((fmtstring = fmtsr.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(fmtstring))
                {
                    sw.WriteLine(fmtstring);
                    continue;
                }

                if (fmtstring.StartsWith(";"))
                {
                    //sw.WriteLine("<!-- {0} -->", fmtstring.From(";").Trim());
                    continue;
                }

                string[] xs = fmtstring.Split(' ');

                MatchId match;
                if (!bracket.Matches.TryGetValue(xs[2], out match))
                {
                    continue;
                }

                WriteBracketLine(sw, match.P1Var, match.P1, match.MatchInfo.P1.Flag);
                WriteBracketLine(sw, match.P2Var, match.P2, match.MatchInfo.P2.Flag);

                var matchNode = match.MatchInfo.Node;
                if (matchNode != null)
                {
                    var special = new string[] { "0", "1", "2", "date", "date1", "bestof", "vetoes", "width", "veto1", "veto2", "race1", "race2", "flag1", "flag2" };

                    sw.WriteLine("|{0}details={{{{BracketMatchSummary", match.MatchVar);
                    if (matchNode.HasParam("date"))
                        sw.WriteLine("|date=" + matchNode.GetParamText("date"));
                    else if (matchNode.HasParam("date1"))
                        sw.WriteLine("|date=" + matchNode.GetParamText("date1"));
                    else
                    {
                        // if the last text was in italics, it's probably the date
                        string ltdate = null;

                        var lt = match.MatchInfo.LastText;
                        if (lt.StartsWith("''") && lt.EndsWith("''"))
                        {
                            ltdate = lt.Substring(2, lt.Length - 4);
                        }

                        if (ltdate != null && MessageBox.Show("Use last text (" + ltdate + ") as date?", "No date field", MessageBoxButtons.YesNo) == DialogResult.Yes)
                            sw.WriteLine("|date=" + ltdate);
                    }

                    // sundries
                    foreach (var key in matchNode.Params.Keys.OrderBy((x) => x))
                    {
                        if (special.Contains(key)
                            || key.StartsWith("map")
                            || key.StartsWith("win")
                            || key.StartsWith("date")
                            || ((key.Length > 3) && key.StartsWith("vod")))
                            continue;

                        //if (key.StartsWith("date") && key != "date1" && !string.IsNullOrWhiteSpace(matchNode.GetParamText(key)))
                        //{
                        //    ConsoleEx.WriteLine(ConsoleColor.Red, "Irregular date format");
                        //    Console.ReadLine();
                        //}

                        WriteParamIfNotNull(sw, matchNode, key);
                    }

                    // maps + vods
                    for (int i = 1; i <= 9; i++)
                    {
                        string mapParam = "map" + i.ToString();
                        string vodText = "";
                        if (matchNode.HasParam(mapParam))
                        {
                            string vodtype1 = matchNode.GetParamText("vod" + i.ToString());
                            string vodtype2 = matchNode.GetParamText("vodgame" + i.ToString());
                            if (!string.IsNullOrWhiteSpace(vodtype1) && vodtype1 != "novod")
                            {
                                vodText = string.Format("|vodgame{0}={{{{vod|gamenum={0}|vod={1}|source={2}}}}}",
                                    i, vodtype1,
                                    matchNode.GetParamText("vod" + i.ToString() + "source"));
                            }
                            else if (!string.IsNullOrWhiteSpace(vodtype2) && vodtype2 != "novod")
                            {
                                vodText = string.Format("|vodgame{0}={{{{vod|gamenum={0}|vod={1}|source=url}}}}",
                                    i, vodtype2);
                            }

                            //{{vod|gamenum=1|vod={{{vod1|}}}|source={{{vod1source|}}}}}

                            if (i != 1)
                                WriteParamIfNotNull(sw, matchNode, "date" + i.ToString());

                            sw.WriteLine("|map{0}={1} |map{0}win={2} {3}",
                                i,
                                matchNode.GetParamText(mapParam),
                                matchNode.GetParamText("win" + i.ToString()),
                                vodText);
                            // |vodgame{0}=
                        }
                    }

                    // vetoes
                    WriteParamIfNotNull(sw, matchNode, "veto1");
                    WriteParamIfNotNull(sw, matchNode, "veto2");

                    sw.WriteLine("}}");
                }
            }
            sw.WriteLine("}}");
            return sw.ToString();
        }
    }

    private static void WriteBracketLine(StringWriter sw, string P1Var, BracketLine P1, string MatchInfoFlag)
    {
        if (P1.Id.Replace("'", "").ToLower() == "bye")
            sw.WriteLine("|{0}={1} |{0}race={2} |{0}flag={3} |{0}score={4} |{0}win={5}",
                P1Var, "BYE", "bye", "", "-", "");
        else
            sw.WriteLine("|{0}={1} |{0}race={2} |{0}flag={3} |{0}score={4} |{0}win={5}",
                P1Var, P1.Id, P1.Race, string.IsNullOrWhiteSpace(P1.Flag) ? MatchInfoFlag : P1.Flag, P1.Score, P1.Win ? "1" : "");
    }

    private static void WriteParamIfNotNull(TextWriter tw, WikiTemplateNode node, string param)
    {
        var value = node.GetParamText(param);
        if (!string.IsNullOrWhiteSpace(value))
            tw.WriteLine("|{0}={1}", param, value);
    }

    private static string GetLookup(BracketLine p1, BracketLine p2)
    {
        return string.Format("{0}_{1}", p1.Id, p2.Id);
    }

    private static BracketInfo AnalyzeBracket(WikiTemplateNode node)
    {
        string fmtfile = Path.Combine("LPfmt", node.Name + ".bracketfmt");
        if (!File.Exists(fmtfile))
        {
            ConsoleEx.WriteLine(ConsoleColor.Red, "Bracket not recognised");
            Console.ReadLine();
            return new BracketInfo();
        }

        BracketInfo info = new BracketInfo();
        info.Node = node;
        info.Name = node.Name;
        info.Matches = new Dictionary<string, MatchId>();

        using (var fmtsr = new StreamReader(fmtfile))
        {
            string fmtstring;
            while ((fmtstring = fmtsr.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(fmtstring) || fmtstring.StartsWith(";"))
                {
                    continue;
                }

                string[] xs = fmtstring.Split(' ');

                var match = new MatchId();
                match.P1Var = xs[0];
                match.P2Var = xs[1];
                match.MatchVar = xs[2];
                match.P1 = ParseBracketLine(node, match.P1Var);
                match.P2 = ParseBracketLine(node, match.P2Var);
                if (!(string.IsNullOrWhiteSpace(match.P1.Id) && string.IsNullOrWhiteSpace(match.P2.Id)))
                    info.Matches.Add(match.MatchVar, match);
            }
        }

        return info;
    }
    private static BracketLine ParseBracketLine(WikiTemplateNode node, string var)
    {
        var line = new BracketLine();
        line.Id = node.GetParamText(var);
        if (line.Id == null) return line;   // entry does not exist
        
        line.Flag = node.GetParamText(var + "flag");
        line.Race = node.GetParamText(var + "race");
        line.Score = node.GetParamText(var + "score");
        line.Win = node.GetParamText(var + "win") == "1";

        if (line.Id.StartsWith("'''") && line.Id.EndsWith("'''"))
        {
            line.Id = line.Id.Substring(3, line.Id.Length - 6);
            line.Win = true;
        }
        return line;
    }

    private static MatchInfo AnalyzeMatch(WikiTemplateNode node)
    {
        var info = new MatchInfo();
        info.Node = node;
        foreach (var param in node.Params)
        {
            ConsoleEx.Write(ConsoleColor.Cyan, "|{0}=", param.Key);
            if (param.Value.Count == 0)
            {
                // no values
                Console.WriteLine();
            }
            else if (param.Value.Count > 1)
            {
                ConsoleEx.WriteLine(ConsoleColor.Red, "(More than one value.)");
                Console.ReadLine();
            }
            else
            {
                if (param.Value[0] is WikiTemplateNode)
                {
                    ConsoleEx.WriteLine(ConsoleColor.Red, "Template value.");
                    Console.ReadLine();
                }
                else
                {
                    var text = (param.Value[0] as WikiTextNode).Text;
                    switch (param.Key)
                    {
                        case "1": info.P1.Id = text; break;
                        case "flag1": info.P1.Flag = text; break;
                        case "race1": info.P1.Race = text; break;
                        case "link1": info.P1.Link = text; break;

                        case "2": info.P2.Id = text; break;
                        case "flag2": info.P2.Flag = text; break;
                        case "race2": info.P2.Race = text; break;
                        case "link2": info.P2.Link = text; break;
                    }
                    Console.WriteLine(text);
                }
            }
        }
        return info;
    }
}
