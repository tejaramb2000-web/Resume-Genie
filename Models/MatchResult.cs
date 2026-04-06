namespace ResumeTailorApp.Models
{
    public class MatchResult
    {
        public int ResumeId { get; set; }
        public int JobId { get; set; }

        public string ResumeTitle { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;

        public int Score { get; set; }

        public List<string> MatchedKeywords { get; set; } = new();
        public List<string> MissingKeywords { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
    }
}