using System;
using static System.Console;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text.RegularExpressions;

namespace trello_pdd
{
    class Program
    {
        private static string ConfigFileName = "pdd.txt";

        static void Main(string[] args)
        {
            var configuration = new Configuration();
            // try to load configuration file
            if(!File.Exists(ConfigFileName))
            {
                WriteLine("No configuration file found, please enter configuration data");
                foreach(var property in configuration.GetType().GetProperties(
                    BindingFlags.Public|BindingFlags.Instance))
                {
                    Write($"{property.Name}: ");
                    var value = ReadLine();
                    property.SetValue(configuration, value);
                }

                WriteLine("Saving configuration");
                configuration.WriteToFile(ConfigFileName);
            }
            else
            {
                var couldLoad = configuration.ReadFromFile(ConfigFileName);
                if(!couldLoad)
                {
                    return;    
                }
            }

            if (!Directory.Exists(configuration.SourceFolder))
            {
                Console.WriteLine($"Source folder '{configuration.SourceFolder}' not found. Exiting application.");
                return;
            }

            var parser = new SourceParser();
            var tasks = parser.ParseSource(configuration.SourceFolder);

            var trelloConnection = new TrelloConnector(configuration.ApiKey, configuration.ApiToken);
            var boardId = trelloConnection.GetBoardByName(configuration.UserId, configuration.BoardName);
            if (boardId == null)
                return;
            var cards = trelloConnection.GetCardsOfBoard(boardId);
            // only load attachements for cards for which we will have to add subtasks
            var parentIds = tasks.Select(x => x.ParentNumber).Distinct();
            var parentCards = cards.Where(x => parentIds.Contains(x.Number));
            var subTasksToCreate = new List<Task>();
            foreach(var parentCard in parentCards)
            {
                var attachments = trelloConnection.GetAttachmentUrlsForCard(parentCard.Id);
                // check if there is an attachment which has the same name as 
                // one of our sub tasks, if yes, the subtask is already generated
                var subTasks = tasks.Where(x => x.ParentNumber == parentCard.Number);
                foreach(var subTask in subTasks)
                {
                    var alreadyLinked = false;
                    foreach(var attachment in attachments)
                    {
                        if(string.Compare(
                            attachment.Title,
                            subTask.TitleWithoutWhiteSpace,true) == 0)
                        {
                            alreadyLinked = true;
                            break;
                        }
                    }
                    if(!alreadyLinked)
                    {
                        subTasksToCreate.Add(subTask);
                    }
                }
            }

            // subtask list contains all tasks that should be created and added to trello
            // create a new card in trello now and add it as attachment to the parent card.

            /// TODO 6: Batch Modus für Lesen der Attachments verwenden
            /// TODO 6: Karten für Subtasks in Trello generieren


            return;
            


        }
    }

    public class Attachment
    {
        private readonly Regex TitleRegex = 
            new Regex(@"s*https:\/\/trello\.com\/c\/[a-zA-Z0-9]*\/\d*-(.*)",
                      RegexOptions.Compiled|RegexOptions.IgnoreCase);
        public string Url { get; set; }
        public string Title { get; private set; }
        public Attachment(string url)
        {
            Url = url;
            var match = TitleRegex.Match(url);
            if(match.Success)
            {
                var encodedTitle = match.Groups[1].Value;
                Title = System.Net.WebUtility.UrlDecode(encodedTitle);
            }
        }
    }

    public class Card
    {
        public string Id { get; set; }
        public int Number { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
    }

    public class TrelloConnector
    {
        private readonly string _apiToken;
        private readonly string _apiKey;
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly string _authenticationParameters;
        private readonly string _baseAddress = "https://api.trello.com/1";

        public TrelloConnector(string apiKey,string apiToken)
        {
            _apiKey = apiKey;
            _apiToken = apiToken;
            //_httpClient.BaseAddress = new Uri("https://api.trello.com/1");
            _authenticationParameters = $"?key={_apiKey}&token={_apiToken}";
        }

        private T GetContentFromTrello<T>(string queryUrl,Func<string,T> processingFunction)
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
