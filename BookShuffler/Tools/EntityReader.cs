using BookShuffler.Models;
using BookShuffler.Tools.Storage;

namespace BookShuffler.Tools
{
    public class EntityReader
    {
        private readonly IStorageProvider _storage;

        public EntityReader(IStorageProvider storage)
        {
            _storage = storage;
        }

        public SerializableSection LoadSection(string file)
        {
            var deserializer = new YamlDotNet.Serialization.Deserializer();
            return deserializer.Deserialize<SerializableSection>(_storage.Get(file));
        }

        public IndexCard LoadIndexCard(string file)
        {
            var deserializer = new YamlDotNet.Serialization.Deserializer();
            var text = _storage.Get(file);
            var parts = text.Split("---", 3);

            // TODO: should this be logged?
            if (parts.Length < 3) return null;

            var cardInfo = deserializer.Deserialize<IndexCard>(parts[1]);
            cardInfo.Content = parts[2].Trim();
            return cardInfo;
        }

    }
}