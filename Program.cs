using System;
using static System.Console;
using System.IO;
using System.Linq;
using TaskList = System.Collections.Generic.Dictionary<string, trello_pdd.Task>;
using System.Text.RegularExpressions;
using System.Collections.Generic;

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
                Write("BoardName: ");
                configuration.BoardName = ReadLine();
                Write("API-Token: ");
                configuration.ApiToken = ReadLine();
                Write("Source Folder: ");
                configuration.SourceFolder = ReadLine();
                while(!Directory.Exists(configuration.SourceFolder))
                {
                    Write($"Source folder {configuration.SourceFolder} does not exist, please enter new one: ");
                    configuration.SourceFolder = ReadLine();
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

            var parser = new SourceParser();
            var tasks = parser.ParseSource(configuration.SourceFolder);

            /// TODO 1: Connect to Trello
            /// Use API Token to connect to Trello and request all Tasks from there.
            /// After that check if new tasks should be created.

            /// TODO 1: Add new task to Trello
            /// Create a new task and add it to the Trello Board.



            // TODO:
            // 1. Load configuration data from file,
            //      - name of trello board
            //      - API Token
            //      - Folder with source files
            // 2. Parse source files, search for /// TODO <number> <task title>
            // 3. When TODO line found, search for all directly following lines with ///
            //      these are the additional comments for the tasks
            //      - create internal list with tasks
            // 4. Add new tasks to trello
            //      - Get list of all available tasks
            //      - Search for tasks with given number
            //      - Check if task already has related tasks with given title
            //      - If not, create  new tasks and add them as attachements to parent task
            // 5. Optional: If all subtasks of a task are done, ask user to also set the parent task to done (recursivly for additional parent tasks?)
            // 6. Optional: If there is an open subtask, but no TODO entry any more and also no more related subtasks, close the task

        }
    }

    public class Configuration
    {
        public string ApiToken { get; set; }
        public string BoardName { get; set; }
        public string SourceFolder { get; set; }

        public void WriteToFile(string fileName)
        {
            File.WriteAllLines(fileName, new string[]
            {
                $"{nameof(ApiToken)}={ApiToken}",
                $"{nameof(BoardName)}={BoardName}",
                $"{nameof(SourceFolder)}={SourceFolder}"
            });
        }

        public bool ReadFromFile(string fileName)
        {
            try
            {
                var content = File.ReadAllLines(fileName);
                foreach(var line in content.Select(x => x.Split('=')))
                {
                    if(line.Length == 2)
                    {
                        var key = line[0];
                        var value = line[1];
                        GetType().GetProperty(key).SetValue(this, value);
                    }
                }
                return true;
            }
            catch(Exception ex)
            {
                WriteLine($"Could not read configuration: {ex}");
                return false;
            }
        }
    }

    public class Task
    {
        public int ParentNumber { get; set; }
        public string Title { get; set; }
        public List<string> Description { get; } = new List<string>();
        public string DescriptionText => string.Join(" ",Description);
    }



    public class SourceParser
    {
        private readonly Regex TaskBeginRegEx = 
            new Regex(@"(\s*)\/\/\/(\s*)TODO(\s*)(\d+):(\s*)(.*)(\s*)",
                      RegexOptions.Compiled|RegexOptions.IgnoreCase);
        private readonly Regex TaskDescriptionRegEx =
            new Regex(@"(\s*)\/\/\/(\s*)(.*)(\s*)",
                      RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public TaskList ParseSource(string sourceFolder)
        {
            var taskList = new TaskList();
            Task currentTask = null;
            // iterate over all source files (currently, we only regard *.cs und *.ts files)
            var sourceFiles =
                Directory.GetFiles(sourceFolder, "*.cs", SearchOption.AllDirectories)
                         .ToList();
            sourceFiles.AddRange(
                Directory.GetFiles(sourceFolder, "*.ts", SearchOption.AllDirectories));
            foreach (var sourceFile in sourceFiles)
            {
                var sourceLines = File.ReadLines(sourceFile).ToList();
                foreach (var sourceLine in sourceLines)
                {
                    // first line of task was found, create new task object
                    var taskBeginMatch = TaskBeginRegEx.Match(sourceLine);
                    if (taskBeginMatch.Success)
                    {
                        var parentNumber = int.Parse(taskBeginMatch.Groups[4].Value);
                        var title = taskBeginMatch.Groups[6].Value;
                        currentTask = new Task
                        {
                            ParentNumber = parentNumber,
                            Title = title
                        };
                        taskList.Add(title, currentTask);
                    }
                    else if (currentTask != null) // if no task found, ignore additional comments
                    {
                        var taskDescriptionMatch = TaskDescriptionRegEx.Match(sourceLine);
                        if (taskDescriptionMatch.Success)
                        {
                            currentTask.Description.Add(taskDescriptionMatch.Groups[3].Value.Trim());
                        }
                        else
                        {
                            // there is a line not starting with /// => task description is completed
                            currentTask = null;
                        }
                    }
                }
            }
            return taskList;
        }
    }
}
