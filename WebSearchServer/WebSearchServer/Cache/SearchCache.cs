using System.Collections.Generic;
using WebSearchServer.Logging;

namespace  WebSearchServer.Cache
{
  public class SearchCache
  {
    private readonly Dictionary<string, CacheEntry> _cache; //Glavni storage za cache
    private readonly LinkedList<string> _lruOrder; // cuva redosled koriscenja LRU strategija
    private readonly object _lock = new object();
    private readonly int _maxSize;
    private readonly Logger _logger = Logger.Instance;

    public SearchCache(int maxSize = 50)
    {
      _cache = new Dictionary<string, CacheEntry>();
      _lruOrder = new LinkedList<string>();
      _maxSize = maxSize;
    }

    public Dictionary<string, Dictionary<string, int>> Get(string key)
    {
      lock (_lock)
      {
        if (!_cache.ContainsKey(key))
        {
          _logger.Cache($"MISS - {key}");
          return null;
        }

        CacheEntry entry = _cache[key];
        _lruOrder.Remove(entry.LruNode);
        _lruOrder.AddFirst(entry.LruNode);

        _logger.Cache($"HIT - {key}");
        return entry.Results;
      }
    }

    public void Set(string key, Dictionary<string, Dictionary<string, int>> results)
    {
      lock (_lock)
      {
        if (_cache.ContainsKey(key))
        {
          CacheEntry existing = _cache[key];
          _lruOrder.Remove(existing.LruNode);
          _lruOrder.AddFirst(key);
          existing.LruNode = _lruOrder.First;
          existing.Results = results;
          return;
        }

        if (_cache.Count >= _maxSize)
        {
          string oldest = _lruOrder.Last.Value;
          _lruOrder.RemoveLast();
          _cache.Remove(oldest);
          _logger.Cache($"Evicted - {oldest}");
        }
        
        LinkedListNode<string> node = _lruOrder.AddFirst(key);
        _cache[key] = new CacheEntry{Results = results, LruNode = node};
        _logger.Cache($"SET - {key}");

      }
    }

    private readonly HashSet<string> _inProgress = new HashSet<string>();
    
    public bool TryMarkInProgress(string key)
    {
      lock (_lock)
      {
        if(_inProgress.Contains(key))
          return false;
        
        _inProgress.Add(key);
        return true;
      }
    }
    
    public void WaitForResult(string key)
    {
      lock (_lock)
      {
        while (_inProgress.Contains(key) && !_cache.ContainsKey(key))
        {
          Monitor.Wait(_lock);
        }
      }
    }

    public void MarkDone(string key)
    {
      lock (_lock)
      {
        _inProgress.Remove(key);
        Monitor.PulseAll(_lock);
      }
    }
  }

  public class CacheEntry
  {
    public Dictionary<string, Dictionary<string, int>> Results { get; set; }//Stvari rezultat pretrage
    public LinkedListNode<string> LruNode { get; set; }// referenca na cvor u LinkedList
  }
}

