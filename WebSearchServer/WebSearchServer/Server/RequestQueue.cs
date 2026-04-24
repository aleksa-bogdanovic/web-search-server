using System.Net;

namespace WebSearchServer.Server
{
    public class RequestQueue
    {
        private readonly Queue<HttpListenerContext> _queue;
        private readonly object _lock = new object();
        private readonly int _maxSize;
        
        public RequestQueue(int maxSize = 100)
        {
            _queue = new Queue<HttpListenerContext>();
            _maxSize = maxSize;
        }

        public void Enqueue(HttpListenerContext context)
        {
            lock (_lock)
            {
                while (_queue.Count >= _maxSize)
                {
                    Monitor.Wait(_lock);
                }
                
                _queue.Enqueue(context);
                Monitor.PulseAll(_lock);
            }
        }

        public HttpListenerContext Dequeue()
        {
            lock (_lock)
            {
                while (_queue.Count == 0)
                {
                    Monitor.Wait(_lock);
                }
                
                HttpListenerContext context = _queue.Dequeue();
                Monitor.PulseAll(_lock);
                return context;
            }
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _queue.Count;
                }
            }
        }
    }
}

