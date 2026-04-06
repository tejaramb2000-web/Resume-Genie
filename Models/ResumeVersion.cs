namespace ResumeTailorApp.Models
{
    public class ResumeVersion
    {
        public int Id { get; set; }

        public int ResumeId { get; set; }
        public Resume? Resume { get; set; }

        public string JobTitle { get; set; } = "";
        public string Content { get; set; } = "";
       public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}