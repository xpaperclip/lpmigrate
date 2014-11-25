using System;
using System.Collections.Generic;
using System.Linq;

namespace LxTools.Liquipedia
{
    public static class LiquipediaUtils
    {
        public static string NormaliseLink(string s)
        {
            if (s == null) return null;
            s = s.Replace(" ", "_");
            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}

namespace LxTools.Liquipedia.Parsing2
{
    public static class StringUtils2
    {
        public static int FirstIndexOf(this string s, int start, out int type, out int delta, params string[] needles)
        {
            type = -1;
            int index = int.MaxValue;
            for (int i = 0; i < needles.Length; i++)
            {
                string needle = needles[i];
                int tryIdx = s.IndexOf(needle, start);
                if ((tryIdx >= 0) && (tryIdx < index))
                {
                    index = tryIdx;
                    type = i;
                }
            }

            if (type != -1)
            {
                delta = index - start;
                return index + needles[type].Length;
            }
            else
            {
                delta = 0;
                return start;
            }
        }
    }

    public static class WikiParser
    {
        public static IEnumerable<WikiNode> Parse(string page)
        {
            return Parse(page, false);
        }
        public static IEnumerable<WikiNode> Parse(string page, bool ignoreTables)
        {
            int idx = 0;
            while (idx < page.Length)
            {
                int start = idx;
                int type, delta;
                if (ignoreTables)
                {
                    idx = page.FirstIndexOf(idx, out type, out delta, "<!--", "{{", "=====", "====", "===", "==");
                    if (type >= 1) type++;
                }
                else
                {
                    idx = page.FirstIndexOf(idx, out type, out delta, "<!--", "{|", "{{", "=====", "====", "===", "==");
                }

                if (type != -1)
                {
                    string text = page.Substring(start, delta).TrimWhitespace();
                    if (!string.IsNullOrEmpty(text))
                        yield return new WikiTextNode(text);
                }

                switch (type)
                {
                    case 0: // <!--
                        yield return ParseTextNonGreedy(page, WikiTextNodeType.Comment, idx, out idx);
                        break;
                    case 1: // {|
                        yield return ParseTable(page, idx, out idx);
                        break;
                    case 2: // {{
                        yield return ParseTemplate(page, idx, out idx);
                        break;
                    case 6: // ==
                        yield return ParseTextNonGreedy(page, WikiTextNodeType.Section2, idx, out idx);
                        break;
                    case 5: // ===
                        yield return ParseTextNonGreedy(page, WikiTextNodeType.Section3, idx, out idx);
                        break;
                    case 4: // ====
                        yield return ParseTextNonGreedy(page, WikiTextNodeType.Section4, idx, out idx);
                        break;
                    case 3: // =====
                        yield return ParseTextNonGreedy(page, WikiTextNodeType.Section5, idx, out idx);
                        break;
                    default: // just text left
                        string rest = page.Substring(idx).TrimWhitespace();
                        if (!string.IsNullOrEmpty(rest))
                            yield return new WikiTextNode(rest);
                        yield break;
                }
            }
        }

        private static WikiTextNode ParseTextNonGreedy(string page, WikiTextNodeType type, int start, out int idx)
        {
            string delimiter;
            switch (type)
            {
                case WikiTextNodeType.Comment: delimiter = "-->"; break;
                case WikiTextNodeType.Section2: delimiter = "=="; break;
                case WikiTextNodeType.Section3: delimiter = "==="; break;
                case WikiTextNodeType.Section4: delimiter = "===="; break;
                case WikiTextNodeType.Section5: delimiter = "====="; break;
                default: throw new ArgumentOutOfRangeException("type");
            }

            WikiTextNode node = new WikiTextNode(type);
            idx = start;
            int closingidx = page.IndexOf(delimiter, idx);
            if (closingidx < 0)
            {
                node.Text = page.Substring(idx).TrimWhitespace();
                idx = page.Length;
            }
            else
            {
                node.Text = page.Substring(idx, closingidx - idx).TrimWhitespace();
                idx = closingidx + delimiter.Length;
            }
            node.Start = start;
            node.Length = idx - start;
            return node;
        }
        private static WikiTemplateNode ParseTemplate(string page, int start, out int idx)
        {
            var node = new WikiTemplateNode();
            node.Start = start;
            idx = start;
            while (idx < page.Length)
            {
                int type, delta;
                start = idx;
                idx = page.FirstIndexOf(start, out type, out delta, "<!--", "{{", "}}");
                node.Length = idx - start;

                if (type != -1)
                {
                    string text = page.Substring(start, delta).TrimWhitespace();
                    if (!string.IsNullOrEmpty(text))
                        node.Children.Add(new WikiTextNode(text));
                }

                switch (type)
                {
                    case 0: // <!--
                        node.Children.Add(ParseTextNonGreedy(page, WikiTextNodeType.Comment, idx, out idx));
                        break;
                    case 1: // {{
                        node.Children.Add(ParseTemplate(page, idx, out idx));
                        break;
                    case 2: // }}
                        return node.MutateParseParams();
                    default: // just text left
                        string rest = page.Substring(idx).TrimWhitespace();
                        if (!string.IsNullOrEmpty(rest))
                        {
                            node.Children.Add(new WikiTextNode(rest));
                        }
                        return node.MutateParseParams();
                }
            }
            return node.MutateParseParams();
        }
        private static WikiTableNode ParseTable(string page, int start, out int idx)
        {
            var node = new WikiTableNode();
            node.Start = start;
            WikiNode newnode;
            List<WikiNode> currentCell = new List<WikiNode>();
            node.Cells.Add(currentCell);
            idx = start;
            while (idx < page.Length)
            {
                int type, delta;
                start = idx;
                idx = page.FirstIndexOf(start, out type, out delta, "<!--", "{{", "{|", "|}", "|-", "!", "|");

                if (type != -1)
                {
                    string text = page.Substring(start, delta).TrimWhitespace();
                    if (!string.IsNullOrEmpty(text))
                    {
                        newnode = new WikiTextNode(text);
                        currentCell.Add(newnode);
                        node.Children.Add(newnode);
                    }
                }

                switch (type)
                {
                    case 0: // <!--
                        newnode = ParseTextNonGreedy(page, WikiTextNodeType.Comment, idx, out idx);
                        currentCell.Add(newnode);
                        node.Children.Add(newnode);
                        break;
                    case 1: // {{
                        newnode = ParseTemplate(page, idx, out idx);
                        currentCell.Add(newnode);
                        node.Children.Add(newnode);
                        break;
                    case 2: // {|
                        newnode = ParseTable(page, idx, out idx);
                        currentCell.Add(newnode);
                        node.Children.Add(newnode);
                        break;
                    case 3: // |}
                        node.Length = idx - start;
                        return node;
                    case 4: // |-
                        // don't do anything
                        break;
                    case 5: // !
                    case 6: // |
                        currentCell = new List<WikiNode>();
                        node.Cells.Add(currentCell);
                        break;
                    default: // just text left
                        string rest = page.Substring(idx).TrimWhitespace();
                        if (!string.IsNullOrEmpty(rest))
                        {
                            newnode = new WikiTextNode(rest);
                            currentCell.Add(newnode);
                            node.Children.Add(newnode);
                        }
                        throw new ArgumentOutOfRangeException("Malformed table.");
                        //return node;
                }
            }
            throw new ArgumentOutOfRangeException("Malformed table.");
            //return node;
        }
    }

    public static class WikiNodeExtensionMethods
    {
        public static bool IsTextNode(this WikiNode node)
        {
            return (node is WikiTextNode);
        }
        public static bool IsTextType(this WikiNode node, WikiTextNodeType type)
        {
            var textNode = node as WikiTextNode;
            if (textNode == null) return false;
            return (textNode.Type == type);
        }
        public static bool IsSection(this WikiNode node)
        {
            var textNode = node as WikiTextNode;
            if (textNode == null) return false;
            if ((textNode.Type != WikiTextNodeType.Section2) &&
                (textNode.Type != WikiTextNodeType.Section3) && 
                (textNode.Type != WikiTextNodeType.Section4) &&
                (textNode.Type != WikiTextNodeType.Section5))
                return false;
            return true;
        }
        public static bool IsSection(this WikiNode node, string text)
        {
            if (!node.IsSection()) return false;
            return ((node as WikiTextNode).Text == text);
        }
    }
    public abstract class WikiNode
    {
        internal WikiNode() { }

        public int Start { get; internal set; }
        public int Length { get; internal set; }
    }
    public enum WikiTextNodeType
    {
        Text, Comment, Section2, Section3, Section4, Section5
    }
    public class WikiTextNode : WikiNode
    {
        internal WikiTextNode() : this(WikiTextNodeType.Text, null) { }
        internal WikiTextNode(WikiTextNodeType type) : this(type, null) { }
        internal WikiTextNode(string text) : this(WikiTextNodeType.Text, text) { }
        internal WikiTextNode(WikiTextNodeType type, string text)
        {
            this.Type = type;
            this.Text = text;
        }

        public WikiTextNodeType Type { get; internal set; }
        public string Text { get; internal set; }
        public override string ToString()
        {
            return string.Format("{0} <{1}>", this.Type, this.Text);
        }
    }
    
    public abstract class WikiContainerNode : WikiNode
    {
        internal WikiContainerNode() { }

        private readonly List<WikiNode> children = new List<WikiNode>();
        public List<WikiNode> Children { get { return children; } }
    }
    public class WikiTemplateNode : WikiContainerNode
    {
        internal WikiTemplateNode() { }

        private readonly Dictionary<string, List<WikiNode>> _params = new Dictionary<string, List<WikiNode>>();
        public Dictionary<string, List<WikiNode>> Params { get { return _params; } }

        public string Name
        {
            get
            {
                List<WikiNode> node;
                if (this.Params.TryGetValue("0", out node))
                {
                    if ((node.Count > 0) && (node[0] is WikiTextNode))
                    {
                        string name = (node[0] as WikiTextNode).Text;
                        if (name.StartsWith("Template:")) name = name.From("Template:");
                        return LiquipediaUtils.NormaliseLink(name);
                    }
                }
                return null;
            }
        }

        internal WikiTemplateNode MutateParseParams()
        {
            int unnamed = 0;
            string paramName = "0";
            List<WikiNode> currentParam = new List<WikiNode>();
            Queue<WikiNode> remainingItems = new Queue<WikiNode>(this.Children);
            while (remainingItems.Count > 0)
            {
                var node = remainingItems.Dequeue();
                if ((node is WikiTextNode) && (node as WikiTextNode).Type == WikiTextNodeType.Text)
                {
                    string text = (node as WikiTextNode).Text;
                    int baridx;
                    while ((baridx = text.IndexOf('|')) >= 0)
                    {
                        // add up to the |
                        string tt = text.Substring(0, baridx).TrimWhitespace();
                        if (!string.IsNullOrEmpty(tt))
                            currentParam.Add(new WikiTextNode(tt));
                        this.Params[paramName] = currentParam;
                        currentParam = new List<WikiNode>();

                        text = text.Substring(baridx + 1);
                        int nextbar = text.IndexOf('|');
                        int equalsidx;
                        if (nextbar > 0)
                            equalsidx = text.IndexOf('=', 0, nextbar);
                        else
                            equalsidx = text.IndexOf('=');
                        if (equalsidx < 0)
                        {
                            unnamed++;
                            paramName = unnamed.ToString();
                        }
                        else
                        {
                            paramName = text.Substring(0, equalsidx).TrimWhitespace();
                            text = text.Substring(equalsidx + 1);
                        }
                    }
                    text = text.TrimWhitespace();
                    if (text.Length > 0) currentParam.Add(new WikiTextNode(text));
                }
                else
                {
                    currentParam.Add(node);
                }
            }
            if (currentParam.Count != 0)
            {
                this.Params[paramName] = currentParam;
            }
            return this;
        }

        public bool HasParam(string label)
        {
            if (label == null || !this.Params.ContainsKey(label)) return false;
            return true;
        }
        public string GetParamText(string label)
        {
            if (label == null || !this.Params.ContainsKey(label)) return null;
            return string.Join(" ", (from item in this.Params[label]
                                     where item is WikiTextNode
                                     let text = item as WikiTextNode
                                     where text.Type == WikiTextNodeType.Text
                                     select text.Text));
        }
        public WikiTemplateNode GetParamTemplate(string label, string template)
        {
            if (label == null || !this.Params.ContainsKey(label)) return null;
            return (from item in this.Params[label]
                    where item is WikiTemplateNode
                    let templ = item as WikiTemplateNode
                    where templ.Name == template
                    select templ).FirstOrDefault();
        }
        public IEnumerable<WikiTemplateNode> GetParamTemplates(string label, string template)
        {
            if (label == null || !this.Params.ContainsKey(label)) return null;
            return (from item in this.Params[label]
                    where item is WikiTemplateNode
                    let templ = item as WikiTemplateNode
                    where templ.Name == template
                    select templ);
        }

        public override string ToString()
        {
            return string.Format("Template <{0}>", this.Name);
        }
    }
    public class WikiTableNode : WikiContainerNode
    {
        internal WikiTableNode() { }

        private readonly List<List<WikiNode>> cells = new List<List<WikiNode>>();
        public List<List<WikiNode>> Cells { get { return cells; } }

        public override string ToString()
        {
            return string.Format("Table <{0} cells>", this.Cells.Count);
        }
    }
}
