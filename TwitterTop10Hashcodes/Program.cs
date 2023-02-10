//-----------------------------------------------------------------------
// <author>James S Wilson</author>
//-----------------------------------------------------------------------

using TwitterTop10Hashtags;
using System.Collections.Concurrent;

var twitterStreamProcessor = new ProcessTwitterStream();
var hashtags = new Hashtags();
var logFrequency = 1;  // In minutes
var timeToLog = DateTime.Now.AddMinutes(logFrequency);
var lastTweetCount = 0;
var tweetMessageQueue = new ConcurrentQueue<string>();
var idleTime = 0;

Console.WriteLine($"Time: {DateTime.Now:g}");
Console.WriteLine($"Starting to log every {logFrequency} minute(s)...");
Console.WriteLine();

try
{
    var processingTask = Task.Run(async () => { 
        while (true)
        {
            while (!tweetMessageQueue.IsEmpty)
            {
                if (tweetMessageQueue.TryDequeue(out string? tweetMessage))
                {
                    hashtags.UpdateHashtagCounts(tweetMessage);
                    LogStatistics();
                }
            }
            idleTime++;
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    });

    await twitterStreamProcessor.GetTweets((Action<TweetObject>)((tweet) =>
    {
        var tweetMessage = tweet?.Data?.Text ?? "";
        tweetMessageQueue.Enqueue(tweetMessage);
        //Console.WriteLine($"ID: {tweet?.Data?.Id}");
        //Console.WriteLine(tweetMessage);

    }));
    await processingTask;
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
        var queueLength = tweetMessageQueue.Count;
        timeToLog = DateTime.Now.AddMinutes(logFrequency);
        Console.WriteLine($"Time: {DateTime.Now:g}  Queue Length: {queueLength}  Idle Time: {idleTime}");
        Console.WriteLine($"Total number of tweets received: {newTweetCount:n0} New: {newTweetCount - lastTweetCount:n0}");
        Console.WriteLine($"Total number of hashtags received: {hashtags.GetNumberOfHashtags():n0}");
        foreach (var hashtag in hashtags.GetTopHashtags().ToList())
        {
            var hashtagLine = String.Format($"{hashtag.Key,-30} {hashtag.Value:n0}");
            Console.WriteLine(hashtagLine);
        }
        Console.WriteLine();
        lastTweetCount = newTweetCount;
        idleTime = 0;
    }
}