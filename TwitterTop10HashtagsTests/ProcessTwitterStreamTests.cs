using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Text;
using TwitterTop10Hashtags;

namespace TwitterTop10HashtagsTests;

public class ProcessTwitterStreamTests
{
    //private readonly ProcessTwitterStream twitterStreamProcessor;

    //public ProcessTwitterStreamTests(string filename)
    //{
    //    HttpClient httpClient = MockHttpClient(filename);

    //    twitterStreamProcessor = new ProcessTwitterStream(httpClient);
    //}

    private static HttpClient MockHttpClient(string filename)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
           .Protected()
           // Setup the PROTECTED method to mock
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>()
           )
           // prepare the expected response of the mocked http call
           .ReturnsAsync(new HttpResponseMessage()
           {
               StatusCode = HttpStatusCode.OK,
               Content = new MyContent(filename)
           })
           .Verifiable();

        // use real http client with mocked handler here
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test.com/"),
        };
        return httpClient;
    }
    public class MyContent : HttpContent
    {
        private readonly string _data;
        public MyContent(string fileName)
        {
            _data = File.ReadAllText($"TestFiles\\{fileName}.json");
        }

        // Minimal implementation needed for an HTTP request content,
        // i.e. a content that will be sent via HttpClient, contains the 2 following methods.
        protected override bool TryComputeLength(out long length)
        {
            // This content doesn't support pre-computed length and
            // the request will NOT contain Content-Length header.
            length = 0;
            return false;
        }

        // SerializeToStream* methods are internally used by CopyTo* methods
        // which in turn are used to copy the content to the NetworkStream.
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => stream.WriteAsync(Encoding.UTF8.GetBytes(_data)).AsTask();

        // Override SerializeToStreamAsync overload with CancellationToken
        // if the content serialization supports cancellation, otherwise the token will be dropped.
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
            => stream.WriteAsync(Encoding.UTF8.GetBytes(_data), cancellationToken).AsTask();

        // In rare cases when synchronous support is needed, e.g. synchronous CopyTo used by HttpClient.Send,
        // implement synchronous version of SerializeToStream.
        protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
            => stream.Write(Encoding.UTF8.GetBytes(_data));

        // CreateContentReadStream* methods, if implemented, will be used by ReadAsStream* methods
        // to get the underlying stream and avoid buffering.
        // These methods will not be used by HttpClient on a custom content.
        // They are for content receiving and HttpClient uses its own internal implementation for an HTTP response content.
        protected override Task<Stream> CreateContentReadStreamAsync()
            => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(_data)));

        // Override CreateContentReadStreamAsync overload with CancellationToken
        // if the content serialization supports cancellation, otherwise the token will be dropped.
        protected override Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken)
            => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(_data))).WaitAsync(cancellationToken);

        // In rare cases when synchronous support is needed, e.g. synchronous ReadAsStream,
        // implement synchronous version of CreateContentRead.
        protected override Stream CreateContentReadStream(CancellationToken cancellationToken)
            => new MemoryStream(Encoding.UTF8.GetBytes(_data));
    }

    [Theory]
    [InlineData("SampleTweets", "")]
    [InlineData("TooManyRequests", "Status: 429 - Too Many Requests")]
    [InlineData("Unauthorized", "Status: 401 - Unauthorized")]
    public async Task GetTweets_ShouldReturnListOfTweets(string filename, string errorMessage)
    {
        // Arange
        var twitterStreamProcessor = new ProcessTwitterStream(MockHttpClient(filename));

        // Act
        Func<Task> act = () => twitterStreamProcessor.GetTweets((Action<TweetObject>)((tweet) =>
        {
            var tweetMessage = tweet?.Data?.Text ?? "";
        }));

        //Assert
        if (!string.IsNullOrEmpty(errorMessage))
        {
            // Has the correct error message
            var exception = await Assert.ThrowsAsync<Exception>(act);
            Assert.Equal(errorMessage, exception.Message);
        }
        else
        {
            // No error message
            Assert.True(string.IsNullOrEmpty(errorMessage));
        }
    }

}
