using System;
using static System.Console;
using System.IO;
using System.Linq;
using System.Reflection;

namespace trello_pdd
{
    public class Configuration
    {
        public string ApiToken { get; set; }
        public string ApiKey { get; set; }
        public string UserId { get; set; }
        public string BoardName { get; set; }
        public string SourceFolder { get; set; }

        public void WriteToFile(string fileName)
        {
            var entries = GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(x => $"{x.Name}={x.GetValue(this)}")
                .ToArray();
            File.WriteAllLines(fileName,entries);
        }

        public bool ReadFromFile(string fileName)
        {
            try
            {
                var content = File.ReadAllLines(fileName);
                foreach (var line in content.Select(x => x.Split('=')))
                {
                    if (line.Length == 2)
                    {
                        var key = line[0];
                        var value = line[1];
                        GetType().GetProperty(key).SetValue(this, value);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                WriteLine($"Could not read configuration: {ex}");
                return false;
            }
        }
    }
}
