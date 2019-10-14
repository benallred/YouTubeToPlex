using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace YouTubeToPlex.Tests
{
	public class FakeHttpMessageHandler : HttpMessageHandler
	{
		private Dictionary<string, string> RequestToResponse { get; } = new Dictionary<string, string>();

		public FakeHttpMessageHandler()
		{
		}

		public FakeHttpMessageHandler(string requestUri, string responseContent)
		{
			Mock(requestUri, responseContent);
		}

		public void Mock(string requestUri, string responseContent)
		{
			RequestToResponse.Add(requestUri, responseContent);
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			if (RequestToResponse.TryGetValue(request.RequestUri.ToString(), out var responseContent))
			{
				return Task.FromResult(new HttpResponseMessage() { Content = new StringContent(responseContent) });
			}
			else
			{
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
			}
		}
	}
}
