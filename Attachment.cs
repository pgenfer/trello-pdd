using System.Text.RegularExpressions;

namespace trello_pdd
{
    public class Attachment
    {
        private readonly Regex TitleRegex =
            new Regex(@"s*https:\/\/trello\.com\/c\/[a-zA-Z0-9]*\/\d*-(.*)",
                      RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public string Url { get; set; }
        public string Title { get; private set; }
        public Attachment(string url)
        {
            Url = url;
            var match = TitleRegex.Match(url);
            if (match.Success)
            {
                var encodedTitle = match.Groups[1].Value;
                Title = System.Net.WebUtility.UrlDecode(encodedTitle);
            }
        }
    }
}
