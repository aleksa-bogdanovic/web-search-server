using System;
using System.IO;
using System.Net;
using System.Threading;
using WebSearchServer.Cache;
using WebSearchServer.Logging;
using WebSearchServer.Response;
using WebSearchServer.Search;
using WebSearchServer.Server;

class Program
{
    static void Main(string[] args)
    {
        Logger logger = Logger.Instance;
        
        string textFilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TextFiles");

        if (!Directory.Exists(textFilesPath))
        {
            logger.Error($"TextFiles folder ne postoji:{textFilesPath}");
            return;
            
        }
        
        logger.Info($"TextFiles folder:  {textFilesPath}");
        
        FileSearcher fileSearcher = new FileSearcher(textFilesPath);
        SearchCache cache = new SearchCache(maxSize:50);
        ResponseBuilder responseBuilder = new ResponseBuilder();
        RequestQueue requestQueue = new RequestQueue(maxSize:100);
        ThreadPoolWorker worker = new ThreadPoolWorker(fileSearcher, cache,responseBuilder);

        int workerCount = 4;
        for (int i = 0; i < workerCount; i++)
        {
            Thread t = new Thread(() =>
            {
                while (true)
                {
                    HttpListenerContext context = requestQueue.Dequeue();
                    ThreadPool.QueueUserWorkItem(worker.ProcessRequest, context);
                }
            });
            t.IsBackground = true;
            t.Start();
        }
        logger.Info($"Pokrenuto : {workerCount} worker niti");

        string prefix = "http://localhost:5050/";
        HttpServer server = new HttpServer(prefix, requestQueue);
        
        logger.Info("Pritisnite Ctrl+C za zaustavljanje servera.");
        server.Start();
    }
}


