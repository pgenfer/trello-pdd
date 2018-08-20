using System.Collections.Generic;

namespace trello_pdd
{
    public class Task
    {
        public int ParentNumber { get; set; }
        public string Title { get; set; }
        public List<string> Description { get; } = new List<string>();
        public string DescriptionText => string.Join(" ", Description);
        // Trello replaces whitespaces in card URLs with "-", so we should also do this here for later matching
        public string TitleWithoutWhiteSpace => Title.Replace(" ", "-");
    }
}
