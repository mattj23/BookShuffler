namespace BookShuffler.Tools.Storage
{
    public interface IStorageProvider
    {
        string Get(string path);

        void Put(string path, string value);

        string[] List(string path);

        string Join(params string[] s);

        void Delete(string path);
    }
}