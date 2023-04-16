namespace Joba.IBM.RPA
{
    public interface ILocalRepository<T> : IEnumerable<T>, ICollection where T : class
    {
        void AddOrUpdate(params T[] items);
        void Update(T item);
        T? Get(string name);
        void Remove(string name);
        void Clear();
    }
}