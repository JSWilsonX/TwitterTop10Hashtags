//-----------------------------------------------------------------------
// <author>James S Wilson</author>
//-----------------------------------------------------------------------

using TwitterTop10Hashtags;
using System.Collections.Concurrent;
using System.Diagnostics;

var twitterStreamProcessor = new ProcessTwitterStream(new HttpClient());
var hashtags = new Hashtags();
var logFrequency = 1;  // In minutes
var timeToLog = DateTime.Now.AddMinutes(logFrequency);
var lastTweetCount = 0;
var tweetMessageQueue = new ConcurrentQueue<string>();
var processingTime = 0L;

Console.WriteLine($"Time: {DateTime.Now:g}");
Console.WriteLine($"Starting to log every {logFrequency} minute(s)...");
Console.WriteLine();

try
{
    // Process tweets asynchronously 
    var processingTask = Task.Run(async () => { 
        while (true)
        {
            var stopwatch = Stopwatch.StartNew();
            while (!tweetMessageQueue.IsEmpty)
            {
                if (tweetMessageQueue.TryDequeue(out string? tweetMessage))
                {
                    hashtags.UpdateHashtagCounts(tweetMessage);
                    LogStatistics();
                }
            }

            // Measure the processing time then wait
            stopwatch.Stop();
            processingTime += stopwatch.ElapsedMilliseconds;
            await Task.Delay(TimeSpan.FromMilliseconds(1000 - stopwatch.ElapsedMilliseconds));
        }
    });

    // Read stream asynchronously 
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
    Console.WriteLine("---");
    LogStatistics();
}

void LogStatistics()
{
    if (timeToLog < DateTime.Now)
    {
        var newTweetCount = hashtags.GetNumberOfTweets();
        timeToLog = DateTime.Now.AddMinutes(logFrequency);
        Console.WriteLine($"Time: {DateTime.Now:g}  Queue Length: {tweetMessageQueue.Count}  Processing Time: {processingTime} ms");
        Console.WriteLine($"Total number of tweets received: {newTweetCount:n0} New: {newTweetCount - lastTweetCount:n0}");
        Console.WriteLine($"Total number of hashtags received: {hashtags.GetNumberOfHashtags():n0}");
        foreach (var hashtag in hashtags.GetTopHashtags().ToList())
        {
            var hashtagLine = String.Format($"{hashtag.Key,-30} {hashtag.Value:n0}");
            Console.WriteLine(hashtagLine);
        }
        Console.WriteLine();
        lastTweetCount = newTweetCount;
        processingTime = 0;
    }
}