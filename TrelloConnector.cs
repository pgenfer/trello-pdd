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
        private List<Label> _labels = new List<Label>();

        public TrelloConnector(string apiKey, string apiToken)
        {
          _apiKey = apiKey;
          _apiToken = apiToken;
          //_httpClient.BaseAddress = new Uri("https://api.trello.com/1");
          _authenticationParameters = $"key={_apiKey}&token={_apiToken}";
        }

        private T GetContentFromTrello<T>(string queryUrl, Func<string, T> processingFunction)
        {
          var response = _httpClient.GetAsync($"{_baseAddress}/{queryUrl}?{_authenticationParameters}");
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
          var result = GetContentFromTrello(
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

        public void LoadLabelsOfBoard(string boardId)
        {
            var result = GetContentFromTrello(
                $"boards/{boardId}/labels",
                x => JArray.Parse(x)
                        .Children<JObject>()
                        .Select(l =>
                                new Label
                                {
                                    Id = l["id"].ToString(),
                                    Name = l["name"].ToString()

                                })
                        .ToList());
            this._labels = result;
        }

        public string GetBoardByName(string userName, string boardName)
        {
          var result = GetContentFromTrello(
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
          var result = GetContentFromTrello(
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


        public string GetListIdByBoardAndName(string boardId, string listName)
        {
          var result = GetContentFromTrello(
            $"/boards/{boardId}/lists",
            x =>
            {
              var lists = JArray.Parse(x);
              var list = lists
                .Children<JObject>()
                .FirstOrDefault(l => string.Compare(l["name"].ToString(), listName, StringComparison.OrdinalIgnoreCase) == 0);
              return list?["id"].ToString();

            });
          return result;
        }

        public Card CreateTrelloCardFromTask(Task task, string listId)
        {
            var response = _httpClient.PostAsync($"{_baseAddress}/cards?name={task.Title}&desc={task.DescriptionText}&idList={listId}&{_authenticationParameters}",null);
          // since we have a console application, there is no need for async processing
          response.Wait();
          if (response.Result.IsSuccessStatusCode)
          {
            var readContent = response.Result.Content.ReadAsStringAsync();
            readContent.Wait();
            var newCardJson = JObject.Parse(readContent.Result);
            var newCard = new Card
            {
              Id = newCardJson["id"].ToString(),
              Title = newCardJson["name"].ToString(),
              Number = (int) newCardJson["idShort"],
              Url = newCardJson["url"].ToString()
            };

                // after the card is created, try to add the labels to it
                if(!string.IsNullOrEmpty(task.Label))
                {
                    var label = this._labels.FirstOrDefault(x => string.Compare(x.Name, task.Label, true) == 0);
                    if(label != null)
                    {
                        response = _httpClient.PostAsync($"{_baseAddress}/cards/{newCard.Id}/idLabels?value={label.Id}&{_authenticationParameters}", null);
                        response.Wait();
                        if(response.Result.IsSuccessStatusCode)
                        {
                            WriteLine($"Label '{task.Label}' was added to card '{newCard.Title}'");    
                        }
                        else
                        {
                            WriteLine($"Error while writing label '{task.Label}': {response.Result.ReasonPhrase}");
                        }
                    }
                    else
                    {
                        WriteLine($"Label '{task.Label}' was not found in Trello. Maybe a typo?");
                    }
                }
            return newCard;
          }
          else
          {
                    WriteLine($"Error while writing card '{task.Title}': {response.Result.ReasonPhrase}");
          }

          return null;
        }

        public void AddCardAsAttachment(Card parent, Card child)
        {
          var response = _httpClient.PostAsync($"{_baseAddress}/cards/{parent.Id}/attachments/?name={child.Url}&url={child.Url}&{_authenticationParameters}", null);
          // since we have a console application, there is no need for async processing
          response.Wait();
          if (response.Result.IsSuccessStatusCode)
          {
            return;
          }
          else
          {
            WriteLine($"Error while adding attachment '{child.Title} to card '{parent.Title}': {response.Result.ReasonPhrase}");
          }
        }
      }
    }
