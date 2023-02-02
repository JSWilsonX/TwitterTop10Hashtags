using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace TwitterTop10Hashtags;

public class Hashtags
{
    readonly string pattern = @"#(\p{L}|\p{M}|\p{N})+";
    private int minValue; // Lowest count in top ten (may be repeated)

    // handle daily hashtag sample count of about 1.25 Million without reallocation
    // Tweeter recomends 1-2 hashtags but higher numbers are common
    private static readonly int concurrencyLevel = 10;
    private static readonly int initialHashtagAllocation = 1500000;
    private ConcurrentDictionary<string, int> tagCounts =
        new(concurrencyLevel: concurrencyLevel, capacity: initialHashtagAllocation);

    private static readonly int topCount = 10;
    private Dictionary<string, int> mostCommon = new(topCount);

    private int numberOfTweets = 0;

    public Hashtags() { }

    /// <summary>
    /// Get the hashtags in a tweet
    /// </summary>
    /// <param name="tweetMessage"></param>
    /// <returns>List of hashtags</returns>
    public List<string> GetHashtags(string tweetMessage)
    {
        return Regex.Matches(tweetMessage, pattern)
            .Select(match => match.Value)
            .ToList();
    }

    /// <summary>
    /// Update Hashtag Counts
    /// </summary>
    /// <param name="tweetMessage"></param>
    public void UpdateHashtagCounts(string tweetMessage)
    {
        // Get hashtags in message
        var hashtags = GetHashtags(tweetMessage);

        // Increment the hashtag counts and return them
        var updatedHashtags = IncrementValues(hashtags);

        // Update the most common list
        UpdateMostCommon(updatedHashtags);

        // Update number of tweets
        numberOfTweets++;
    }

    /// <summary>
    /// Get most common hashtags
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, int> GetTopHashtags() => mostCommon;

    /// <summary>
    /// Get number of tweets processed
    /// </summary>
    /// <returns></returns>
    public int GetNumberOfTweets() => numberOfTweets;

    /// <summary>
    /// Get number of tweets processed
    /// </summary>
    /// <returns></returns>
    public int GetNumberOfHashtags() => tagCounts.Count;

    /// <summary>
    /// Update the most common list
    /// </summary>
    /// <param name="updatedHashtags"></param>
    private void UpdateMostCommon(Dictionary<string, int> updatedHashtags)
    {
        // Find updates in most common list
        var needUpdatesInTopTen = mostCommon.Keys.Intersect(updatedHashtags.Keys);
        if (needUpdatesInTopTen.Any())
        {
            Parallel.ForEach(needUpdatesInTopTen, (hashtag) =>
            {
                mostCommon[hashtag] = updatedHashtags[hashtag];
            });
        }

        // Find updates not already in most common list
        var lowerHashtags = updatedHashtags.Keys.Except(needUpdatesInTopTen);
        if (lowerHashtags.Any())
        {
            var newTopTen = mostCommon.ToList();

            // Find hashtags with counts higher than the lowest in most common list
            newTopTen.AddRange(updatedHashtags.Where(hashtag => hashtag.Value > minValue));

            // Do we need to replace something in the most common list?
            if (newTopTen.Count > Math.Min(topCount, mostCommon.Count))
            {
                mostCommon = newTopTen.OrderByDescending(newTopTen => newTopTen.Value)
                    .ToList().Take(topCount)
                    .ToDictionary(common => common.Key, x => x.Value);

                // Update the lowest value in the most common list
                minValue = mostCommon.Last().Value;
            }
        }
    }

    /// <summary>
    /// Function to increment the value of a key in the dictionary
    /// and return the nunber of times the hashtag was seen
    /// </summary>
    /// <param name="hashtag"></param>
    /// <returns>The nunber of times the hashtag was seen</returns>
    private int IncrementValue(string hashtag)
    {
        return tagCounts.AddOrUpdate(hashtag, 1, (existingKey, existingValue) => existingValue + 1);
    }

    /// <summary>
    /// Function to increment the values of a list of key in the dictionary
    /// and returns a list of updated hashtags and their new values
    /// </summary>
    /// <param name="hashtags"></param>
    /// <returns>A list of updated hashtags and their new values</returns>
    private Dictionary<string, int> IncrementValues(List<string> hashtags)
    {
        var updatedKeys = new Dictionary<string, int>();
        Parallel.ForEach(hashtags, (hashtag) => {
            updatedKeys[hashtag] = IncrementValue(hashtag);
        });
        return updatedKeys;
    }

}
