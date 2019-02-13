using System;
using System.IO;
using System.Linq;
using TaskList = System.Collections.Generic.List<trello_pdd.Task>;
using System.Text.RegularExpressions;

namespace trello_pdd
{
    public class SourceParser
    {
        private readonly Regex TaskBeginRegEx =
            //new Regex(@"\s*\/\/\/\s*TODO\s*(\d*):\s*(.*)\s*",
            // format is: /// TODO label number, label and number are optional
            new Regex(@"\s*\/\/\/\s*TODO\s*(\w*)\s*(\d*)\s*:\s*(.*)\s*",
                      RegexOptions.Compiled | RegexOptions.IgnoreCase);
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
                        var label = taskBeginMatch.Groups[1].Value;
                        // check if we have a parent number, if not
                        // the task will be a root task
                        var parentNumber = taskBeginMatch.Groups[2].Length > 0 ?
                                                         (int?)int.Parse(taskBeginMatch.Groups[2].Value) :
                                                         null;
                        var title = taskBeginMatch.Groups[3].Value;

                        currentTask = new Task
                        {
                            ParentNumber = parentNumber,
                            Title = title,
                            Label = label
                        };
                        taskList.Add(currentTask);
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
