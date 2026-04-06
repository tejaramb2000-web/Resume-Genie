using System.Collections.Generic;

namespace ResumeTailorApp.Models.ViewModels
{
    public class MatchResultViewModel
    {
        public int ResumeId { get; set; }
        public int JobId { get; set; }

        public string ResumeTitle { get; set; } = string.Empty;
        public string ResumeContent { get; set; } = string.Empty;

        public string JobTitle { get; set; } = string.Empty;
        public string JobDescription { get; set; } = string.Empty;

        public int MatchScore { get; set; }

        public List<string> MatchedKeywords { get; set; } = new List<string>();
        public List<string> MissingKeywords { get; set; } = new List<string>();
        public List<string> Suggestions { get; set; } = new List<string>();

        public List<Resume> Resumes { get; set; } = new List<Resume>();
        public List<Job> Jobs { get; set; } = new List<Job>();
    }
}