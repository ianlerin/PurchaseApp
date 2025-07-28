using PurchaseBlazorApp2.Components.Data;

namespace PurchaseBlazorApp2.Components.Global
{
    public class ClientStateStorage
    {
        public Dictionary<string, object> _stateMap = new Dictionary<string, object>();

        public void Set<T>(string key, T value)
        {
            _stateMap[key] = value;
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (_stateMap.TryGetValue(key, out var obj) && obj is T casted)
            {
                value = casted;
                return true;
            }

            value = default!;
            return false;
        }

        public T Get<T>(string key)
        {
            return TryGet<T>(key, out var value) ? value : default!;
        }

        public void Remove(string key)
        {
            _stateMap.Remove(key);
        }

        public void Clear()
        {
            _stateMap.Clear();
        }
    }
}
