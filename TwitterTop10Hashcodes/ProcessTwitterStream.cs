//-----------------------------------------------------------------------
// <author>James S Wilson</author>
//-----------------------------------------------------------------------

using Newtonsoft.Json;
using System.Net;
namespace TwitterTop10Hashtags;

class ProcessTwitterStream
{
    private readonly HttpClient httpClient;
    private readonly string twitterStreamUrl = "https://api.twitter.com/2/tweets/sample/stream";
    private readonly SecurityProtocolType securityProtocol =
        SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
    private int retryAttempts = 0;
    private const int maxRetryAttempts = 5;

    public ProcessTwitterStream()
    {
        var twitterBearerToken = Environment.GetEnvironmentVariable("TwitterBearerToken");
        var unescapedTwitterToken = Uri.UnescapeDataString(twitterBearerToken ?? "");

        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", unescapedTwitterToken);
    }

    public async Task GetTweets(Action<TweetObject> processTweet)
    {
        while (retryAttempts < maxRetryAttempts)
        {
            try
            {
                var streamResponse = await httpClient.GetAsync(twitterStreamUrl, HttpCompletionOption.ResponseHeadersRead);

                using (var stream = await streamResponse.Content.ReadAsStreamAsync())
                using (var sr = new StreamReader(stream))
                {
                    ServicePointManager.SecurityProtocol = securityProtocol;

                    while (!sr.EndOfStream)
                    {
                        var tweetJson = sr.ReadLine();
                        if (!String.IsNullOrEmpty(tweetJson))
                        {
                            try
                            {
                                var tweetObject = JsonConvert.DeserializeObject<TweetObject>(tweetJson);
                                if (tweetObject?.Data != null)
                                {
                                    processTweet(tweetObject);
                                }
                                else
                                {
                                    throw new Exception("Not a TweetObject");
                                }
                            }
                            catch (Exception)
                            {
                                var errorResponse = JsonConvert.DeserializeObject<TweetError>(tweetJson);
                                if (errorResponse?.Status is 401 or 429)
                                {
                                    Console.WriteLine($"Status: {errorResponse.Status} - {errorResponse.Title}");
                                    throw new Exception($"Status: {errorResponse.Status} - {errorResponse.Title}");
                                }
                                else
                                {
                                    // Keep alive signal received. Do nothing.
                                }
                            }
                        }
                    }
                }
                retryAttempts = 0;
            }
            catch (Exception e)
            {
                if (e.Message != "An error occurred while sending the request.")
                {
                    throw new Exception(e.Message);
                }
                else
                {
                    // This reconnection logic will attempt to reconnect when a disconnection is detected.
                    // To avoid rate limits, this logic implements exponential backoff, so the wait time
                    // will increase if the client cannot reconnect to the stream.
                    await Task.Delay((int)Math.Pow(2, retryAttempts) * 1000);
                    Console.WriteLine("A connection error occurred. Reconnecting...");
                    retryAttempts++;
                }
            }
        }
        Console.WriteLine("Maximum retry attempts reached. Exiting program.");
    }
}
