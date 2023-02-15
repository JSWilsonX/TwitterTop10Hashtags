using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Text;
using TwitterTop10Hashtags;

namespace TwitterTop10HashtagsTests;

public class ProcessTwitterStreamTests
{
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
               Content = new MyHttpContent(filename)
           })
           .Verifiable();

        // use real http client with mocked handler here
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test.com/"),
        };
        return httpClient;
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
