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
      if (!File.Exists(ConfigFileName))
      {
        WriteLine("No configuration file found, please enter configuration data");
        foreach (var property in configuration.GetType().GetProperties(
            BindingFlags.Public | BindingFlags.Instance))
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
        if (!couldLoad)
        {
          return;
        }
      }

      if (!Directory.Exists(configuration.SourceFolder))
      {
        WriteLine($"Source folder '{configuration.SourceFolder}' not found. Exiting application.");
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
      var subTasksToCreate = new Dictionary<Card,List<Task>>();
      foreach (var parentCard in parentCards)
      {
        WriteLine($"Checking parent task '{parentCard.Title}'");
        subTasksToCreate.Add(parentCard, new List<Task>());
        var attachments = trelloConnection.GetAttachmentUrlsForCard(parentCard.Id);
        // check if there is an attachment which has the same name as 
        // one of our sub tasks, if yes, the subtask is already generated
        var subTasks = tasks.Where(x => x.ParentNumber == parentCard.Number);
        foreach (var subTask in subTasks)
        {
          var alreadyLinked = false;
          foreach (var attachment in attachments)
          {
            if (string.Compare(
                attachment.Title,
                subTask.TitleWithoutWhiteSpace, StringComparison.OrdinalIgnoreCase) == 0)
            {
              WriteLine($"\t'{subTask.Title}' already added => Skipped.");
              alreadyLinked = true;
              break;
            }
          }
          if (!alreadyLinked)
          {
            subTasksToCreate[parentCard].Add(subTask);
          }
        }
      }

      var listId = trelloConnection.GetListIdByBoardAndName(boardId, configuration.ListName);

      foreach (var subTaskToCreate in subTasksToCreate)
      {
        if (subTaskToCreate.Value.Count > 0)
        {
          WriteLine($"Creating subtasks for '{subTaskToCreate.Key.Title}'");
          foreach (var subTask in subTaskToCreate.Value)
          {
            WriteLine($"\t{subTask.Title}");
            var subCard = trelloConnection.CreateTrelloCardFromSubTask(subTask, listId);
            trelloConnection.AddCardAsAttachment(subTaskToCreate.Key, subCard);
          }
        }
      }


      // subtask list contains all tasks that should be created and added to trello
      // create a new card in trello now and add it as attachment to the parent card.

      /// TODO 6: Batch Modus für Lesen der Attachments verwenden
      /// TODO 6: Karten für Subtasks in Trello generieren
      /// TODO 6: SubTasks als Attachments hinzufügen
      /// TODO 6: Einen Task mit Description erzeugen
      /// In der Description stehen zusätzliche Informationen für den Task
      /// TODO 21: Ein weiterer SubTasks wird zu einem SubTask hinzugefügt
      /// TODO 21: Some very important task
      /// some very important description
      /// TODO 20: Add Service here
      /// TODO 8: REST API implementieren
      return;



    }
  }
}
