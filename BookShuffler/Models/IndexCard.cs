using YamlDotNet.Serialization;

namespace BookShuffler.Models
{
    public class IndexCard : Entity
    {  
        [YamlIgnore]
        public string? Content { get; set; }

        public int CategoryId { get; set; }
    }
}