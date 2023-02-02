//-----------------------------------------------------------------------
// <author>James S Wilson</author>
//-----------------------------------------------------------------------
using TwitterTop10Hashtags;

var twitterStreamProcessor = new ProcessTwitterStream();
var hashtags = new Hashtags();
var logFrequency = 1;  // In minutes
var timeToLog = DateTime.Now.AddMinutes(logFrequency);
var lastTweetCount = 0;

Console.WriteLine($"Time: {DateTime.Now:g}");
Console.WriteLine($"Starting to log every {logFrequency} minute(s)...");
Console.WriteLine();

try
{
    await twitterStreamProcessor.GetTweets((Action<TweetObject>)((tweet) =>
    {
        var tweetMessage = tweet?.Data?.Text ?? "";
        //Console.WriteLine($"ID: {tweet?.Data?.Id}");
        //Console.WriteLine(tweetMessage);

        hashtags.UpdateHashtagCounts(tweetMessage);
        LogStatistics();
    }));
}
catch (Exception requestException)
{

    Console.WriteLine(requestException.Message);
}

void LogStatistics()
{
    if (timeToLog < DateTime.Now)
    {
        var newTweetCount = hashtags.GetNumberOfTweets();
        timeToLog = DateTime.Now.AddMinutes(logFrequency);
        Console.WriteLine($"Time: {DateTime.Now:g}");
        Console.WriteLine($"Total number of tweets received: {newTweetCount:n0} New: {newTweetCount - lastTweetCount:n0}");
        Console.WriteLine($"Total number of hashtags received: {hashtags.GetNumberOfHashtags():n0}");
        foreach (var hashtag in hashtags.GetTopHashtags().ToList())
        {
            var hashtagLine = String.Format($"{hashtag.Key,-30} {hashtag.Value:n0}");
            Console.WriteLine(hashtagLine);
        }
        Console.WriteLine();
        lastTweetCount = newTweetCount;
    }
}