using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;

namespace Topgg.ProxyCheck
{
    /// <summary>
    /// Dictionary that deserializes IP addresses keys as <see cref="IPAddress"/> and values as
    /// <see cref="ProxyCheckResult.IpResult"/>.
    /// </summary>
    internal class IpResultDictionary : IDictionary<string, JsonElement>
    {
        private readonly Dictionary<string, JsonElement> _extensionData = new();
        private readonly Dictionary<IPAddress, ProxyCheckResult.IpResult> _results;

        public IpResultDictionary(Dictionary<IPAddress, ProxyCheckResult.IpResult> results)
        {
            _results = results;
        }

        public IEnumerator<KeyValuePair<string, JsonElement>> GetEnumerator()
        {
            foreach (var kvp in _results)
            {
                yield return new KeyValuePair<string, JsonElement>(
                    kvp.Key.ToString(), 
                    JsonSerializer.SerializeToDocument(
                        kvp.Value, ProxyJsonContext.Default.IpResult).RootElement);
            }

            foreach (var kvp in _extensionData)
            {
                yield return kvp;
            }
        }

        public void Add(KeyValuePair<string, JsonElement> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _results.Clear();
            _extensionData.Clear();
        }

        public bool Contains(KeyValuePair<string, JsonElement> item)
        {
            return ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<string, JsonElement>[] array, int arrayIndex)
        {
            foreach (var kvp in _results)
            {
                array[arrayIndex++] = new KeyValuePair<string, JsonElement>(
                    kvp.Key.ToString(), 
                    JsonSerializer.SerializeToDocument(
                        kvp.Value, ProxyJsonContext.Default.IpResult).RootElement);
            }

            foreach (var kvp in _extensionData)
            {
                array[arrayIndex++] = kvp;
            }
        }

        public bool Remove(KeyValuePair<string, JsonElement> item)
        {
            return Remove(item.Key);
        }

        public int Count => _results.Count + _extensionData.Count;

        public bool IsReadOnly => false;

        public void Add(string key, JsonElement value)
        {
            if (IPAddress.TryParse(key, out var ip))
            {
                _results.Add(ip, value.Deserialize(ProxyJsonContext.Default.IpResult)!);
            }
            else
            {
                _extensionData.Add(key, value);
            }
        }

        public bool ContainsKey(string key)
        {
            return IPAddress.TryParse(key, out var ip)
                ? _results.ContainsKey(ip)
                : _extensionData.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            return IPAddress.TryParse(key, out var ip)
                ? _results.Remove(ip)
                : _extensionData.Remove(key);
        }

        public bool TryGetValue(string key, out JsonElement value)
        {
            if (_results.TryGetValue(IPAddress.Parse(key), out var result))
            {
                value = JsonSerializer.SerializeToDocument(
                    result, ProxyJsonContext.Default.IpResult).RootElement;
                return true;
            }

            value = default;
            return false;
        }

        public JsonElement this[string key]
        {
            get => IPAddress.TryParse(key, out var ip)
                ? JsonSerializer.SerializeToDocument(
                    _results[ip], ProxyJsonContext.Default.IpResult).RootElement
                : _extensionData[key];

            set
            {
                if (IPAddress.TryParse(key, out var ip))
                {
                    _results[ip] = value.Deserialize(ProxyJsonContext.Default.IpResult)!;
                }
                else
                {
                    _extensionData[key] = value;
                }
            }
        }

        ICollection<string> IDictionary<string, JsonElement>.Keys => _results.Keys
            .Select(ip => ip.ToString())
            .Concat(_extensionData.Keys)
            .ToList();

        ICollection<JsonElement>  IDictionary<string, JsonElement>.Values => _results.Values
            .Select(result => JsonSerializer.SerializeToDocument(
                result, ProxyJsonContext.Default.IpResult).RootElement)
            .Concat(_extensionData.Values)
            .ToList();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
