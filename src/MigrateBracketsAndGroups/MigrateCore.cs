using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LxTools;
using LxTools.Liquipedia;
using LxTools.Liquipedia.Parsing2;

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

struct Player
{
    public string Id;
    public string Flag;
    public string Race;
    public string Link;
}
struct GameSet
{
    public Player P1;
    public Player P2;
    public string Map;
    public string MapLink;
    public string Win;
}

public static class MigrateCore
{
    public static string AnalyzeAndMigrateGroups(string wikicode)
    {
        var wiki = WikiParser.Parse(wikicode, true);

        var interested = new string[] { "Match", "MatchSummary", "GroupTableStart", "GameSet", "Vod", "Vodlink", "Player", "Playersp" };
        var section = "";
        string lastdate = null, date = "";
        bool multidate = false;
        int matchno = 1;
        GameSet lastgs = new GameSet();
        WikiTemplateNode lastvodnode = null;
        bool inMatchList = false;
        bool inMatchMaps = false;
        int mapno = 0, p1wins = 0, p2wins = 0;

        using (StringWriter sw = new StringWriter())
        {
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
                        foreach (string nt in node.Text.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                        {
                            string nodetext = nt.Trim();
                            if (nodetext.StartsWith("''"))
                            {
                                nodetext = StripFormatting(nodetext);
                                ConsoleEx.WriteLine(ConsoleColor.Magenta, "New date");
                                if (MessageBox.Show("Use last text (" + nodetext + ") as date?", "Detected date", MessageBoxButtons.YesNo) == DialogResult.Yes)
                                {
                                    if (lastdate != null) multidate = true;
                                    lastdate = date;
                                    date = nodetext;
                                }
                            }

                            if (nodetext.Contains("#") || nodetext.Contains("*") || nodetext.Contains("<li>"))
                            {
                                //ConsoleEx.WriteLine(ConsoleColor.Magenta, "New set");
                                lastvodnode = null;
                            }

                            ConsoleEx.WriteLine(ConsoleColor.Gray, nodetext);
                        }
                    }
                }
                if (n is WikiTemplateNode)
                {
                    var node = n as WikiTemplateNode;
                    if (node.Name.EndsWith("Bracket") || interested.Contains(node.Name))
                    {
                        Console.WriteLine("{{" + node.Name + "}}");
                        if (node.Name == "Vod" || node.Name == "Vodlink")
                        {
                            lastvodnode = node;
                        }
                        if (node.Name == "GameSet")
                        {
                            var gs = AnalyzeGameSet(node);
                            if (!IsSameMatchup(gs, lastgs))
                            {
                                ConsoleEx.WriteLine(ConsoleColor.Magenta, "New matchup");
                                if (inMatchMaps) WriteMatchMapsEnd(sw, p1wins, p2wins);
                                sw.WriteLine("|match{0}={{{{MatchMaps", matchno);
                                if (!string.IsNullOrWhiteSpace(date))
                                {
                                    sw.WriteLine("|date={0}", date);
                                    date = "";
                                }
                                matchno++;
                                inMatchMaps = true;
                                mapno = 1;
                                p1wins = 0;
                                p2wins = 0;

                                sw.WriteLine(FormatMatchMapsPlayer(gs.P1, "1"));
                                sw.WriteLine(FormatMatchMapsPlayer(gs.P2, "2"));
                            }

                            string win = node.GetParamText("win");
                            sw.Write("|map{0}={1} |map{0}win={2} ",
                                mapno, gs.Map ?? "TBD", node.GetParamText("skip") == "true" ? "skip" : node.GetParamText("win"));
                            if (win == "1") p1wins++;
                            if (win == "2") p2wins++;
                            sw.WriteLine(WriteVod(mapno, lastvodnode));
                            mapno++;

                            lastgs = gs;
                        }
                        if (node.Name == "Match" || node.Name == "MatchSummary")
                        {
                            var analyzedMatch = AnalyzeMatch(node);
                            sw.Write("|match{0}=", matchno);
                            WriteMatchMaps(sw, analyzedMatch);
                            matchno++;
                        }
                        if (node.Name == "GroupTableStart")
                        {
                            ConsoleEx.WriteLine(ConsoleColor.Magenta, "New set of matches: {0}", node.GetParamText("1"));
                            //lastdate = null;
                            //date = "";
                            //multidate = false;
                            matchno = 1;
                            lastgs = new GameSet();
                            if (inMatchMaps) WriteMatchMapsEnd(sw, p1wins, p2wins);
                            if (inMatchList) WriteMatchListEnd(sw);
                            sw.WriteLine("{{{{MatchList |width={0} |title={1}",
                                node.GetParamText("width") ?? "300px",
                                node.GetParamText("1") + " Matches");
                            inMatchList = true;
                            inMatchMaps = false;
                        }
                    }
                }
            }
            if (inMatchMaps) WriteMatchMapsEnd(sw, p1wins, p2wins);
            if (inMatchList) WriteMatchListEnd(sw);
            return sw.ToString();
        }
    }
    private static Player GetPlayer(WikiTemplateNode node, string param)
    {
        var playerNode = node.GetParamTemplate(param, "Player");
        if (playerNode != null) return AnalyzePlayer(playerNode);

        playerNode = node.GetParamTemplate(param, "Playersp");
        if (playerNode != null) return AnalyzePlayer(playerNode);

        ConsoleEx.WriteLine(ConsoleColor.Red, "Unknown player template");
        return new Player() { Id = node.GetParamText(param) };
    }
    private static Player AnalyzePlayer(WikiTemplateNode node)
    {
        Player pl = new Player();
        pl.Id = node.GetParamText("1");
        pl.Link = node.GetParamText("link");
        if (pl.Link == "false") pl.Link = null;
        pl.Race = node.GetParamText("race");
        pl.Flag = node.GetParamText("flag");
        return pl;
    }
    private static GameSet AnalyzeGameSet(WikiTemplateNode node)
    {
        GameSet gs = new GameSet();
        gs.P1 = GetPlayer(node, "1");
        gs.P2 = GetPlayer(node, "2");
        gs.Map = node.GetParamText("map");
        gs.MapLink = node.GetParamText("maplink");
        gs.Win = node.GetParamText("win");
        if (node.GetParamText("skip") != null) gs.Win = "skip";
        return gs;
    }
    private static string WriteVod(int mapno, WikiTemplateNode vodNode)
    {
        if (vodNode == null) return "";
        
        string vodText = "";
        if (vodNode.Name == "Vod")
        {
            if (!string.IsNullOrWhiteSpace(vodNode.GetParamText("novod")))
                return "";

            vodText = string.Format("{{{{vod|gamenum={0}|vod={1}|source={2}}}}}",
                mapno, vodNode.GetParamText("vod"), vodNode.GetParamText("source"));
        }
        if (vodNode.Name == "Vodlink")
        {
            if (!string.IsNullOrWhiteSpace(vodNode.GetParamText("vod-id")))
                vodText = string.Format("{{{{vod|gamenum={0}|vod={1}|source={2}}}}}",
                    mapno, vodNode.GetParamText("vod-id"), "tlpd");
            if (!string.IsNullOrWhiteSpace(vodNode.GetParamText("vpath")))
                vodText = string.Format("{{{{vod|gamenum={0}|vod={1}|source={2}}}}}",
                    mapno, vodNode.GetParamText("vpath"), "URL");
        }

        return string.Format("|vodgame{0}={1}", mapno, vodText);
    }
    private static bool IsSameMatchup(GameSet gs1, GameSet gs2)
    {
        return ((gs1.P1.Id == gs2.P1.Id) && (gs1.P2.Id == gs2.P2.Id))
            || ((gs1.P1.Id == gs2.P2.Id) && (gs1.P2.Id == gs2.P1.Id));
    }
    private static void WriteMatchListEnd(TextWriter sw)
    {
        sw.WriteLine("}}");
        sw.WriteLine();
    }
    private static void WriteMatchMaps(TextWriter sw, MatchInfo match)
    {
        var matchNode = match.Node;
        if (matchNode != null)
        {
            var special = new string[] { "0", "1", "2", "date", "date1", "bestof", "vetoes", "width", "veto1", "veto2", "race1", "race2", "flag1", "flag2" };

            sw.WriteLine("{{MatchMaps");
            sw.WriteLine(FormatMatchMapsPlayer(match.P1, "1"));
            sw.WriteLine(FormatMatchMapsPlayer(match.P2, "2"));
            //|walkover=
            //|mapX=      |mapXwin=       |vodgameX=

            if (matchNode.HasParam("date"))
                sw.WriteLine("|date=" + StripFormatting(matchNode.GetParamText("date")));
            else if (matchNode.HasParam("date1"))
                sw.WriteLine("|date=" + StripFormatting(matchNode.GetParamText("date1")));
            //else
            //{
            //    // if the last text was in italics, it's probably the date
            //    string ltdate = null;

            //    var lt = match.LastText;
            //    if (lt.StartsWith("''") && lt.EndsWith("''"))
            //    {
            //        ltdate = StripFormatting(lt);
            //    }

            //    if (ltdate != null && MessageBox.Show("Use last text (" + ltdate + ") as date?", "No date field", MessageBoxButtons.YesNo) == DialogResult.Yes)
            //        sw.WriteLine("|date=" + ltdate);
            //}

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
            int p1wins = 0;
            int p2wins = 0;
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

                    string map = matchNode.GetParamText(mapParam);
                    string win = matchNode.GetParamText("win" + i.ToString());
                    if (string.IsNullOrWhiteSpace(matchNode.GetParamText(mapParam)))
                    {
                        if (string.IsNullOrWhiteSpace(win) || win == "skip")
                            continue;
                        else
                            map = "TBD";
                    }

                    sw.WriteLine("|map{0}={1} |map{0}win={2} {3}",
                        i, map, win, vodText);
                    // |vodgame{0}=

                    if (win == "1") p1wins++;
                    if (win == "2") p2wins++;
                }
            }

            // vetoes
            WriteParamIfNotNull(sw, matchNode, "veto1");
            WriteParamIfNotNull(sw, matchNode, "veto2");

            WriteMatchMapsEnd(sw, p1wins, p2wins);
        }
    }
    private static string FormatMatchMapsPlayer(BracketLine pl, string num)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendFormat("|player{0}={1} ", num, pl.Id);
        if (!string.IsNullOrEmpty(pl.Flag)) sb.AppendFormat("|player{0}flag={1} ", num, pl.Flag);
        if (!string.IsNullOrEmpty(pl.Race)) sb.AppendFormat("|player{0}race={1} ", num, pl.Race);
        if (!string.IsNullOrEmpty(pl.Link) && pl.Link != "false") sb.AppendFormat("|player{0}link={1} ", num, pl.Link);
        return sb.ToString();
    }
    private static string FormatMatchMapsPlayer(Player pl, string num)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendFormat("|player{0}={1} ", num, pl.Id);
        if (!string.IsNullOrEmpty(pl.Flag)) sb.AppendFormat("|player{0}flag={1} ", num, pl.Flag);
        if (!string.IsNullOrEmpty(pl.Race)) sb.AppendFormat("|player{0}race={1} ", num, pl.Race);
        if (!string.IsNullOrEmpty(pl.Link) && pl.Link != "false") sb.AppendFormat("|player{0}link={1} ", num, pl.Link);
        return sb.ToString();
    }
    private static void WriteMatchMapsEnd(TextWriter sw, int p1wins, int p2wins)
    {
        if (p1wins > p2wins) sw.WriteLine("|winner=1");
        else if (p1wins < p2wins) sw.WriteLine("|winner=2");
        else sw.WriteLine("|winner=");

        sw.WriteLine("}}");
    }


    private static void WriteParamIfNotNull(TextWriter tw, WikiTemplateNode node, string param)
    {
        WriteParamIfNotNull(tw, node, param, true);
    }
    private static void WriteParamIfNotNull(TextWriter tw, WikiTemplateNode node, string param, bool stripFormatting)
    {
        var value = node.GetParamText(param);
        if (!string.IsNullOrWhiteSpace(value))
        {
            if (stripFormatting)
                value = StripFormatting(value);
            tw.WriteLine("|{0}={1}", param, value);
        }
    }
    private static string StripFormatting(string s)
    {
        try
        {
            if (s.StartsWith("'''") && s.EndsWith("'''"))
                return s.Substring(3, s.Length - 6);
            if (s.StartsWith("''") && s.EndsWith("''"))
                return s.Substring(2, s.Length - 4);
            return s;
        }
        catch       // I'm lazy
        {
            return s;
        }
    }


    public static string AnalyzeAndMigrateBrackets(string wikicode)
    {
        var wiki = WikiParser.Parse(wikicode, true);

        var interested = new string[] { "Match", "MatchSummary", "GameSet", "Vod", "Vodlink", "Player", "Playersp" };
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
            sw.WriteLine("{{" + bracket.Name + " ");

            // bracket sundries
            WriteParamIfNotNull(sw, bracket.Node, "column-width");
            WriteParamIfNotNull(sw, bracket.Node, "hideroundtitles");
            for (int i = 1; i <= 7; i++)
            {
                WriteParamIfNotNull(sw, bracket.Node, "R" + i.ToString());
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
                        sw.WriteLine("|date=" + StripFormatting(matchNode.GetParamText("date")));
                    else if (matchNode.HasParam("date1"))
                        sw.WriteLine("|date=" + StripFormatting(matchNode.GetParamText("date1")));
                    else
                    {
                        // if the last text was in italics, it's probably the date
                        string ltdate = null;

                        var lt = match.MatchInfo.LastText;
                        if (lt.StartsWith("''") && lt.EndsWith("''"))
                        {
                            ltdate = StripFormatting(lt);
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
