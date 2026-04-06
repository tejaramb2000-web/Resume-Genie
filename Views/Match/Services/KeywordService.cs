using System.Text.RegularExpressions;

namespace ResumeTailorApp.Services
{
    public class KeywordService
    {
        private readonly HashSet<string> _stopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "the","and","or","a","an","to","of","in","on","for","with","is","are",
            "as","by","at","from","be","this","that","it","will","can","you","your",
            "we","our","they","their","them","was","were","been","has","have","had"
        };

        private readonly HashSet<string> _techKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            // ☁️ CLOUD
            "aws","azure","gcp","cloud","ec2","s3","lambda","rds","dynamodb","redshift",
            "athena","glue","cloudformation","eks","emr","cloudwatch","iam","vpc",

            // 🧱 DATA ENGINEERING
            "etl","elt","data","pipeline","pipelines","datawarehouse","warehouse",
            "databricks","spark","hadoop","hdfs","mapreduce","kafka","flink",
            "airflow","snowflake","delta","lake","datalake","bigquery",

            // 🗄️ DATABASES
            "sql","mysql","postgres","postgresql","oracle","sqlserver",
            "nosql","mongodb","cassandra","redis","elasticsearch","neo4j",

            // 💻 PROGRAMMING
            "python","java","csharp",".net","golang","scala","javascript","typescript",
            "bash","powershell","r","ruby","php","kotlin","swift","c","cpp",

            // 🌐 WEB
            "api","rest","restful","graphql","microservices","backend","frontend",
            "mvc","webapi","asp.net","react","angular","vue","node","express","html","css",

            // ⚙️ DEVOPS
            "docker","kubernetes","terraform","jenkins","ci","cd","ci/cd",
            "ansible","github","gitlab","bitbucket","devops","prometheus",
            "grafana","splunk","monitoring","logging",

            // 📊 ANALYTICS
            "analytics","reporting","dashboard","tableau","powerbi","looker",
            "ssrs","ssis","ssas","excel",

            // 🧠 AI / ML
            "machinelearning","ml","ai","deeplearning","nlp","tensorflow",
            "pytorch","scikitlearn","model","prediction","featureengineering",

            // 🔐 SECURITY
            "security","authentication","authorization","oauth","jwt","encryption","ssl",

            // 🧩 ARCHITECTURE
            "architecture","architect","design","scalable","distributed",
            "systemdesign","eventdriven","highavailability",

            // 🧪 TESTING
            "testing","unittest","integrationtest","automation","selenium","junit","nunit",

            // 📦 STREAMING
            "messaging","streaming","event","pubsub","queue","rabbitmq",

            // 🔧 GENERAL TECH
            "optimization","performance","debugging","troubleshooting",
            "deployment","scaling","integration","automation","agile","scrum"
        };

        public List<string> ExtractKeywords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            var matches = Regex.Matches(text.ToLower(), @"[a-zA-Z\.\+#]+");

            var words = matches
                .Select(m => Normalize(m.Value))
                .Where(w => !_stopWords.Contains(w))
                .Where(w => w.Length > 2)
                .Where(w => _techKeywords.Contains(w))
                .Distinct()
                .ToList();

            return words;
        }

        private string Normalize(string word)
        {
            word = word.ToLower().Trim();

            return word switch
            {
                "amazon" => "aws",
                "amazonwebservices" => "aws",
                "msazure" => "azure",
                "gcloud" => "gcp",
                "nodejs" => "node",
                "reactjs" => "react",
                "angularjs" => "angular",
                "dotnet" => ".net",
                "c#" => "csharp",
                _ => Singularize(word)
            };
        }

        private string Singularize(string word)
        {
            if (word.EndsWith("s") && word.Length > 3)
                return word[..^1];

            return word;
        }
    }
}