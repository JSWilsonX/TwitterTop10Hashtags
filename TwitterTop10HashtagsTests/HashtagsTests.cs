//-----------------------------------------------------------------------
// <author>James S Wilson</author>
//-----------------------------------------------------------------------

namespace TwitterTop10HashtagsTests;
public class HashtagsTests
{
    private readonly Hashtags _hashtags;

    public HashtagsTests()
    {
        _hashtags = new Hashtags();
    }

    [Fact]
    public void GetHashtags_ShouldReturnListOfHashtags()
    {
        // Arrange
        var tweetMessage = "This is a tweet with #hashtags #xunit #test";

        // Act
        var result = _hashtags.GetHashtags(tweetMessage);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("#hashtags", result);
        Assert.Contains("#xunit", result);
        Assert.Contains("#test", result);
    }

    [Fact]
    public void UpdateHashtagCounts_ShouldIncrementHashtagCount()
    {
        // Arrange
        var tweetMessage = "This is a tweet with #hashtags #xunit #test";

        // Act
        _hashtags.UpdateHashtagCounts(tweetMessage);

        // Assert
        Assert.Equal(1, _hashtags.GetNumberOfTweets());
        Assert.Equal(3, _hashtags.GetNumberOfHashtags());
        var topHashtags = _hashtags.GetTopHashtags();
        Assert.Equal(3, topHashtags.Count);
        Assert.Equal(1, topHashtags["#hashtags"]);
        Assert.Equal(1, topHashtags["#xunit"]);
        Assert.Equal(1, topHashtags["#test"]);
    }

    [Fact]
    public void UpdateHashtagCounts_ShouldUpdateTopHashtags()
    {
        // Arrange
        var tweetMessage1 = "This is a tweet with #hashtags #xunit #test";
        var tweetMessage2 = "This is another tweet with #hashtags #xunit";

        // Act
        _hashtags.UpdateHashtagCounts(tweetMessage1);
        _hashtags.UpdateHashtagCounts(tweetMessage2);

        // Assert
        Assert.Equal(2, _hashtags.GetNumberOfTweets());
        Assert.Equal(3, _hashtags.GetNumberOfHashtags());
        var topHashtags = _hashtags.GetTopHashtags();
        Assert.Equal(3, topHashtags.Count);
        Assert.Equal(2, topHashtags["#hashtags"]);
        Assert.Equal(2, topHashtags["#xunit"]);
        Assert.Equal(1, topHashtags["#test"]);
    }
}
