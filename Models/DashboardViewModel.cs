namespace ResumeTailorApp.Models
{
    public class DashboardViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public int ResumeCount { get; set; }
        public int JobCount { get; set; }
        public int VersionCount { get; set; }
        public List<ResumeVersion> LatestVersions { get; set; } = new();
    }
}