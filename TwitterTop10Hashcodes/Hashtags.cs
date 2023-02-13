//-----------------------------------------------------------------------
// <author>James S Wilson</author>
//-----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace TwitterTop10Hashtags;

public class Hashtags
{
    readonly string pattern = @"(?:^|\s)#[\p{L}\p{N}_]+";
    private int minValue; // Lowest count in top ten (may be repeated)

    // handle daily hashtag sample count of about 1.25 Million without reallocation
    // Tweeter recomends 1-2 hashtags but higher numbers are common
    private static readonly int initialHashtagAllocation = 1500000;
    private Dictionary<string, int> tagCounts = new(initialHashtagAllocation);

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
            .Select(match => match.Value.TrimStart())
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
        var lowerHashtags = updatedHashtags.Keys.Except(needUpdatesInTopTen);
        if (needUpdatesInTopTen.Any())
        {
            foreach (var hashtag in needUpdatesInTopTen)
            {
                mostCommon[hashtag] = updatedHashtags[hashtag];
            }

            // If no more updates need to be made then sort and update
            if (!lowerHashtags.Any())
            {
                SortMostCommonAndUpdateMinValue();
            }
        }

        // Find updates not already in most common list
        if (lowerHashtags.Any())
        {
            var commonPlusNew = mostCommon.ToList();

            // Find hashtags with counts higher than the lowest in most common list
            commonPlusNew.AddRange(lowerHashtags
                .ToDictionary(hashtag => hashtag, hashtag => updatedHashtags[hashtag])
                .Where(hashtag => hashtag.Value > minValue));

            // Do we need to replace something in the most common list?
            if (commonPlusNew.Count > Math.Min(topCount, mostCommon.Count))
            {
                mostCommon = commonPlusNew.ToDictionary(hashtag => hashtag.Key, hashtag => hashtag.Value);
                SortMostCommonAndUpdateMinValue();
            }
        }
    }

    private void SortMostCommonAndUpdateMinValue()
    {
        mostCommon = mostCommon.OrderByDescending(hashtag => hashtag.Value)
            .ToList().Take(topCount)
            .ToDictionary(hashtag => hashtag.Key, hashtag => hashtag.Value);
        minValue = mostCommon.Last().Value;
    }

    /// <summary>
    /// Function to increment the value of a key in the dictionary
    /// and return the nunber of times the hashtag was seen
    /// </summary>
    /// <param name="hashtag"></param>
    /// <returns>The nunber of times the hashtag was seen</returns>
    private int IncrementValue(string hashtag)
    {
        if (!tagCounts.TryGetValue(hashtag, out int value))
        {
            value = 0;
        }

        tagCounts[hashtag] = ++value;
        return value;
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
        foreach (var hashtag in hashtags)
        {
            updatedKeys[hashtag] = IncrementValue(hashtag);
        }
        return updatedKeys;
    }

}
