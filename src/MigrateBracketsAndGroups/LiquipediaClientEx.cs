using System;
using System.Net;
using System.IO;
using System.Xml.Linq;

namespace LxTools.Liquipedia
{
    public class LiquipediaClientEx
    {
        private CookieContainer cookies = new CookieContainer();

        public void Login(string username, string password)
        {
            string xml = MakeRequest("format=xml&action=login&lgname={0}&lgpassword={1}", username, password);
            string token = XDocument.Parse(xml).Element("api").Element("login").Attribute("token").Value;
            MakeRequest("format=xml&action=login&lgname={0}&lgpassword={1}&lgtoken={2}", username, password, token);
            MakeRequest("format=xml&action=query&meta=userinfo&uiprop=email");
        }
        public string GetEditToken()
        {
            string xml = MakeRequestGet("format=xml&action=tokens&type=edit");
            return XDocument.Parse(xml).Element("api").Element("tokens").Attribute("edittoken").Value;
        }
        public void EditPage(string title, string text, string summary, string token)
        {
            MakeRequest("format=xml&action=edit&title={0}&text={1}&summary={2}&token={3}", Uri.EscapeDataString(title), Uri.EscapeDataString(text), Uri.EscapeDataString(summary), Uri.EscapeDataString(token));
        }

        public static string RequestParse(string page)
        {
            string xml = MakeRequestStatic("format=xml&action=parse&text={0}", Uri.EscapeDataString(page));
            return XDocument.Parse(xml).Element("api").Element("parse").Element("text").Value;
        }
        public static string GetPageContent(string page)
        {
            string xml = MakeRequestStatic("format=xml&action=query&prop=revisions&rvprop=content&titles={0}", Uri.EscapeDataString(page));
            return XDocument.Parse(xml).Element("api").Element("query").Element("pages").Element("page").Element("revisions").Element("rev").Value;
        }

        private string MakeRequest(string query, params string[] args)
        {
            return MakeRequest(string.Format(query, args));
        }
        private string MakeRequest(string query)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create("http://wiki.teamliquid.net/starcraft2/api.php");
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CookieContainer = cookies;
            using (var sw = new StreamWriter(request.GetRequestStream()))
            {
                sw.Write(query);
            }

            var response = request.GetResponse();
            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                return sr.ReadToEnd();
            }
        }
        private string MakeRequestGet(string query, params string[] args)
        {
            return MakeRequestGet(string.Format(query, args));
        }
        private string MakeRequestGet(string query)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create("http://wiki.teamliquid.net/starcraft2/api.php?" + query);
            request.Method = "GET";
            request.CookieContainer = cookies;

            var response = request.GetResponse();
            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                return sr.ReadToEnd();
            }
        }

        private static string MakeRequestStatic(string query, params string[] args)
        {
            return MakeRequestStatic(string.Format(query, args));
        }
        private static string MakeRequestStatic(string query)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create("http://wiki.teamliquid.net/starcraft2/api.php");
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            using (var sw = new StreamWriter(request.GetRequestStream()))
            {
                sw.Write(query);
            }

            var response = request.GetResponse();
            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                return sr.ReadToEnd();
            }
        }
    }
}
