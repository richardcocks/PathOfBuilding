using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Tests.Modifiers;

public class KeywordFlagHelperTests
{
    [Fact]
    public void NoModFlags_AlwaysMatches()
    {
        Assert.True(KeywordFlagHelper.MatchKeywordFlags(KeywordFlag.Fire, KeywordFlag.None));
        Assert.True(KeywordFlagHelper.MatchKeywordFlags(KeywordFlag.None, KeywordFlag.None));
    }

    [Fact]
    public void AnyMatch_SingleFlag()
    {
        Assert.True(KeywordFlagHelper.MatchKeywordFlags(KeywordFlag.Fire, KeywordFlag.Fire));
        Assert.False(KeywordFlagHelper.MatchKeywordFlags(KeywordFlag.Cold, KeywordFlag.Fire));
    }

    [Fact]
    public void AnyMatch_MultipleFlags()
    {
        // Mod requires Fire|Cold, query has Fire -> matches (OR logic)
        var modFlags = KeywordFlag.Fire | KeywordFlag.Cold;
        Assert.True(KeywordFlagHelper.MatchKeywordFlags(KeywordFlag.Fire, modFlags));
        Assert.True(KeywordFlagHelper.MatchKeywordFlags(KeywordFlag.Cold, modFlags));
        Assert.False(KeywordFlagHelper.MatchKeywordFlags(KeywordFlag.Lightning, modFlags));
    }

    [Fact]
    public void MatchAll_RequiresAllFlags()
    {
        var modFlags = KeywordFlag.Fire | KeywordFlag.Spell | KeywordFlag.MatchAll;

        // Has both -> matches
        Assert.True(KeywordFlagHelper.MatchKeywordFlags(
            KeywordFlag.Fire | KeywordFlag.Spell, modFlags));

        // Has Fire + Spell + extra -> still matches
        Assert.True(KeywordFlagHelper.MatchKeywordFlags(
            KeywordFlag.Fire | KeywordFlag.Spell | KeywordFlag.Hit, modFlags));

        // Missing Spell -> fails
        Assert.False(KeywordFlagHelper.MatchKeywordFlags(KeywordFlag.Fire, modFlags));
    }

    [Fact]
    public void NoQueryFlags_ModHasFlags_Fails()
    {
        Assert.False(KeywordFlagHelper.MatchKeywordFlags(KeywordFlag.None, KeywordFlag.Fire));
    }

    [Fact]
    public void CacheProducesSameResults()
    {
        KeywordFlagHelper.ClearCache();

        // First call populates cache
        bool result1 = KeywordFlagHelper.MatchKeywordFlags(KeywordFlag.Fire | KeywordFlag.Cold, KeywordFlag.Fire);
        // Second call uses cache
        bool result2 = KeywordFlagHelper.MatchKeywordFlags(KeywordFlag.Fire | KeywordFlag.Cold, KeywordFlag.Fire);

        Assert.Equal(result1, result2);
        Assert.True(result1);
    }
}
