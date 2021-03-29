using YamlDotNet.Serialization;

namespace BookShuffler.Models
{
    public class IndexCard : Entity
    {  
        [YamlIgnore]
        public string? Content { get; set; }
    }
}