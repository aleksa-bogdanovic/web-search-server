using System;
using System.Net;
using System.Threading;
using WebSearchServer.Logging;

namespace WebSearchServer.Server;

public class HttpServer
{
    private readonly HttpListener _listener;
    private readonly RequestQueue _requestQueue;
    private readonly string _prefix;
    private readonly Logger _logger = Logger.Instance;
    private bool _running = false;

    public HttpServer(string prefix, RequestQueue requestQueue)
    {
        _prefix = prefix;
        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);
        _requestQueue = requestQueue;
    }

    public void Start()
    {
        _listener.Start();
        _running = true;
        _logger.Info($"[HttpServer] Server pokrenut na {_prefix}");

        while (_running)
        {
            try
            {
                HttpListenerContext context = _listener.GetContext();
                _logger.Info($"[HttpServer] primljen zahtev: {context.Request.Url}");
                _requestQueue.Enqueue(context);
            }
            catch (HttpListenerException e)
            {
                if (_running)
                    _logger.Error($"[HttpServer] Greska: {e.Message}");
            }
        }
    }

    public void Stop()
    {
        _running = false;
        _listener.Stop();
        _logger.Info($"[HttpServer] Server je zaustavljen.");
    }

    public static List<string> ParseKeywords(string rawUrl)
    {
        if(string.IsNullOrEmpty(rawUrl))
            return new List<string>();
        string path = rawUrl.TrimStart('/');
        string[] parts = path.Split("&");

        var keywords = new List<string>();
        foreach (string part in parts)
        {
            string kw = part.Trim();
            if(!string.IsNullOrEmpty(kw))
                keywords.Add(kw);
        }

        return keywords;
    }
}

