using WebSearchServer.Cache;
using WebSearchServer.Logging;
using WebSearchServer.Response;
using WebSearchServer.Search;
using System.Net;

namespace WebSearchServer.Server
{
    public class ThreadPoolWorker
    {
        private readonly FileSearcher _fileSearcher;
        private readonly SearchCache _cache;
        private readonly ResponseBuilder _responseBuilder;
        private readonly Logger _logger= Logger.Instance;


        public ThreadPoolWorker(FileSearcher fileSearcher, SearchCache cache, ResponseBuilder responseBuilder)
        {
            _fileSearcher = fileSearcher;
            _cache = cache;
            _responseBuilder = responseBuilder;
        }

        public void ProcessRequest(object state)
        {
            HttpListenerContext context = (HttpListenerContext)state;
            
            try
            {
                string rawUrl = context.Request.Url.AbsolutePath.TrimEnd('/');
                List<string> keywords = new List<string>(rawUrl.Split('&'));
                string cacheKey = string.Join("&", keywords);
                
                _logger.Info($"Obrada zahteva - kljucne reci: {cacheKey}");
                
                Dictionary<string, Dictionary<string, int>> results = _cache.Get(cacheKey);

                if (results == null)
                {
                    if (_cache.TryMarkInProgress(cacheKey))
                    {
                        try
                        {
                            results = _fileSearcher.Search(keywords);
                            _cache.Set(cacheKey, results);
                        }
                        finally
                        {
                            _cache.MarkDone(cacheKey);
                        }
                    }
                    else
                    {
                        _cache.WaitForResult(cacheKey);
                        results = _cache.Get(cacheKey);
                        
                    }
                }
                
                string html = _responseBuilder.BuildHtml(results,keywords);
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(html);
                
                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();
                
                _logger.Info($"Zahtev obradjen: -{cacheKey}");
            }
            catch (Exception e)
            {
                _logger.Error($"Greska pri obradi zahteva: {e.Message}");
            }
        }

    }
}