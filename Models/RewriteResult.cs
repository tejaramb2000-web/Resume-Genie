namespace ResumeTailorApp.Models
{
    public class RewriteResult
    {
        public string ResumeTitle { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;

        public string OriginalContent { get; set; } = string.Empty;
        public string RewrittenContent { get; set; } = string.Empty;

        // 🔥 ADD THESE (VERY IMPORTANT)
        public string OriginalContentText { get; set; } = string.Empty;
        public string RewrittenContentText { get; set; } = string.Empty;

        public string OriginalContentHtml { get; set; } = string.Empty;
        public string RewrittenContentHtml { get; set; } = string.Empty;
    }
}