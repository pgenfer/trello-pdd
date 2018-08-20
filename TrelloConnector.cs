using System;
using static System.Console;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace trello_pdd
{
    public class TrelloConnector
    {
        private readonly string _apiToken;
        private readonly string _apiKey;
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly string _authenticationParameters;
        private readonly string _baseAddress = "https://api.trello.com/1";

        public TrelloConnector(string apiKey, string apiToken)
        {
            _apiKey = apiKey;
            _apiToken = apiToken;
            //_httpClient.BaseAddress = new Uri("https://api.trello.com/1");
            _authenticationParameters = $"?key={_apiKey}&token={_apiToken}";
        }

        private T GetContentFromTrello<T>(string queryUrl, Func<string, T> processingFunction)
        {
            var response = _httpClient.GetAsync($"{_baseAddress}/{queryUrl}{_authenticationParameters}");
            // since we have a console application, there is no need for async processing
            response.Wait();
            if (response.Result.IsSuccessStatusCode)
            {
                var readContent = response.Result.Content.ReadAsStringAsync();
                readContent.Wait();
                var result = processingFunction(readContent.Result);
                return result;
            }
            else
            {
                WriteLine($"Error while requesting url '{queryUrl}': {response.Result.ReasonPhrase}");
                return default(T);
            }
        }

        public List<Card> GetCardsOfBoard(string boardId)
        {
            var result = GetContentFromTrello<List<Card>>(
                $"boards/{boardId}/cards",
                x => JArray.Parse(x)
                        .Children<JObject>()
                        .Select(c =>
                                new Card
                                {
                                    Id = c["id"].ToString(),
                                    Title = c["name"].ToString(),
                                    Number = (int)c["idShort"],
                                    Url = c["url"].ToString()
                                })
                .ToList());
            return result;
        }

        public string GetBoardByName(string userName, string boardName)
        {
            var result = GetContentFromTrello<string>(
                $"/members/{userName}/boards",
                x =>
            {
                var boards = JArray.Parse(x);
                var board = boards
                    .Children<JObject>()
                    .FirstOrDefault(b => b.ContainsKey("name") && b["name"].ToString() == boardName);
                if (board == null)
                {
                    WriteLine($"Board with name {boardName} was not found");
                    return null;
                }
                return board["id"].ToString();
            });
            return result;
        }

        public List<Attachment> GetAttachmentUrlsForCard(string cardId)
        {
            var result = GetContentFromTrello<List<Attachment>>(
                $"/cards/{cardId}/attachments",
                x =>
                {
                    var attachments = JArray.Parse(x);
                    var attachmentLinks = attachments
                        .Children<JObject>()
                        .Where(a => !(bool)a["isUpload"])
                    .Select(a => new Attachment(a["url"].ToString()))
                        .ToList();
                    return attachmentLinks;

                });
            return result;
        }
    }
}
