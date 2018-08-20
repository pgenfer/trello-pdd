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
            new Regex(@"(\s*)\/\/\/(\s*)TODO(\s*)(\d+):(\s*)(.*)(\s*)",
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
                        var parentNumber = int.Parse(taskBeginMatch.Groups[4].Value);
                        var title = taskBeginMatch.Groups[6].Value;
                        currentTask = new Task
                        {
                            ParentNumber = parentNumber,
                            Title = title
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
