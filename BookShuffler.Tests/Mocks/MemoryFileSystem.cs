using System.Collections.Generic;
using System.Linq;
using BookShuffler.Tools.Storage;

namespace BookShuffler.Tests.Mocks
{
    public class MemoryFileSystem : IStorageProvider
    {
        public MemoryFileSystem()
        {
            this.Values = new Dictionary<string, string>();
        }

        public MemoryFileSystem(Dictionary<string, string> values)
        {
            this.Values = values;
        }

        public Dictionary<string, string> Values { get; }

        public string Get(string path)
        {
            return this.Values[path];
        }

        public void Put(string path, string value)
        {
            this.Values[path] = value;
        }

        public string[] List(string path)
        {
            return this.Values.Keys.Where(k => k.StartsWith(path)).ToArray();
        }

        public string Join(params string[] s)
        {
            return string.Join("/", s);
        }

        public void Delete(string path)
        {
            var toRemove = this.List(path);
            foreach (var s in toRemove)
            {
                this.Values.Remove(s);
            }
        }
    }
}