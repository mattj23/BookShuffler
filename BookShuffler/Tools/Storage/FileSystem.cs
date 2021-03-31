using System;
using System.IO;
using System.Linq;

namespace BookShuffler.Tools.Storage
{
    public class FileSystem : IStorageProvider
    {
        public string Get(string path)
        {
            return File.ReadAllText(path);
        }

        public void Put(string path, string value)
        {
            var directory = new FileInfo(path).DirectoryName;
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

            File.WriteAllText(path, value);
        }

        public string[] List(string path)
        {
            return Directory.EnumerateFiles(path).ToArray();
        }

        public string PrefixOf(string path)
        {
            return new FileInfo(path).DirectoryName ?? throw new InvalidOperationException();
        }

        public string Join(params string[] s)
        {
            return Path.Combine(s);
        }

        public void Delete(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                return;
            }

            if (Directory.Exists(path))
            {
                Directory.Delete(path);
            }

        }
        
    }
}