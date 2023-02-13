//-----------------------------------------------------------------------
// <author>James S Wilson</author>
//-----------------------------------------------------------------------

using Newtonsoft.Json;
using System.IO.Compression;
using System.Net;
namespace TwitterTop10Hashtags;

class ProcessTwitterStream
{
    private readonly HttpClient httpClient;
    private readonly string twitterStreamUrl = "https://api.twitter.com/2/tweets/sample/stream";
    private readonly SecurityProtocolType securityProtocol =
        SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
    private int retryAttempts = 0;
    private const int maxRetryAttempts = 15;

    // Int to Ordinal
    public static string IntToOrdinal(int num)
    {
        if (num <= 0) return num.ToString();

        switch (num % 100)
        {
            case 11:
            case 12:
            case 13:
                return num + "th";
        }

        switch (num % 10)
        {
            case 1:
                return num + "st";
            case 2:
                return num + "nd";
            case 3:
                return num + "rd";
            default:
                return num + "th";
        }
    }
    public ProcessTwitterStream()
    {
        var twitterBearerToken = Environment.GetEnvironmentVariable("TwitterBearerToken");

        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
        httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", twitterBearerToken);
    }

    public async Task GetTweets(Action<TweetObject> processTweet)
    {
        while (retryAttempts < maxRetryAttempts)
        {
            try
            {
                var streamResponse = await httpClient.GetAsync(twitterStreamUrl, HttpCompletionOption.ResponseHeadersRead);

                using (var stream = await streamResponse.Content.ReadAsStreamAsync())
                using (var decompressed = new GZipStream(stream, CompressionMode.Decompress))
                using (var sr = new StreamReader(decompressed))
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
                                    retryAttempts = 0;
                                }
                                else
                                {
                                    throw new Exception("Not a TweetObject");
                                }
                            }
                            catch (Exception e)
                            {
                                // if we don't have the entire json, then we need to get it
                                if (e.Message == "Unexpected end when reading JSON. Path '', line 1, position 1.")
                                {
                                    while (!sr.EndOfStream)
                                    {
                                        tweetJson += sr.ReadLine();
                                    }
                                }

                                else if (e.Message.StartsWith("Unexpected character encountered while parsing"))
                                {
                                    while (!sr.EndOfStream)
                                    {
                                        tweetJson += sr.ReadLine();
                                    }
                                }

                                var errorResponse = JsonConvert.DeserializeObject<TweetError>(tweetJson);

                                // 0 Other error - e.g.: This stream is currently at the maximum allowed connection limit.
                                // 401 Unauthorized
                                // 429 Too many requests
                                if (errorResponse?.Status is 0 or 401 or 429 && !string.IsNullOrEmpty(errorResponse.Detail))
                                {
                                    throw new Exception($"Status: {errorResponse.Status} - {errorResponse.Detail}");
                                }
                                else
                                {
                                    Console.WriteLine($"Keep alive: {e.Message}");
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
                if (e.Message != "An error occurred while sending the request." &&
                    e.Message != "Status: 0 - This stream is currently at the maximum allowed connection limit." &&
                    e.Message != "Unable to read data from the transport connection: An established connection was aborted by the software in your host machine..")
                {
                    throw new Exception(e.Message);
                }
                else
                {
                    // This reconnection logic will attempt to reconnect when a disconnection is detected.
                    // To avoid rate limits, this logic implements exponential backoff, so the wait time
                    // will increase if the client cannot reconnect to the stream.
                    await Task.Delay((int)Math.Pow(2, retryAttempts) * 1000);
                    retryAttempts++;
                    Console.WriteLine($"Reconnecting {IntToOrdinal(retryAttempts)} try...Error: {e.Message}");
                }
            }
        }
        Console.WriteLine("Maximum retry attempts reached. Exiting program.");
    }
}
