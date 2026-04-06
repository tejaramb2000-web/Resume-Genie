namespace ResumeTailorApp.Models
{
    public class Resume
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string FileName { get; set; } = "";
    }
}