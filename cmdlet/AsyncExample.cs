using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Management.Automation;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace cmdlet
{
    [Cmdlet(VerbsDiagnostic.Test, "AsyncExample")]
    public class AsyncExampleCmdlet : Cmdlet
    {
        private readonly HttpClient _httpClient;

        private readonly List<Task> _tasks;

        private readonly ConcurrentQueue<HttpStatusCode> _results;

        public AsyncExampleCmdlet()
        {
            _httpClient = new HttpClient();
            _tasks = new List<Task>();
            _results = new ConcurrentQueue<HttpStatusCode>();
        }

        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public Uri[] Uri { get; set; }

        protected override void ProcessRecord()
        {
            if (Uri is not null)
            {
                foreach (Uri uri in Uri)
                {
                    _tasks.Add(GetUriAsync(uri));
                }
            }

            while (_results.TryDequeue(out HttpStatusCode result))
            {
                WriteObject(result);
            }
        }

        protected override void EndProcessing()
        {
            Task.WaitAll(_tasks.ToArray());

            while (_results.TryDequeue(out HttpStatusCode result))
            {
                WriteObject(result);
            }
        }

        private async Task GetUriAsync(Uri uri)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(uri);
            _results.Enqueue(response.StatusCode);
        }
    }
}