using System.Text.Json;
using ElectionResults.Core.Entities;
using ElectionResults.Core.Extensions;
using FluentAssertions;

namespace ElectionResults.Tests;

public class PartiesExtensionsShould
{
  private readonly List<Party> _parties = JsonSerializer.Deserialize<List<Party>>(TestData.SerializedParties);
    
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData(null)]
    public void ReturnNullWhenShortnameIsNullOrEmpty(string testValue)
    {
        _parties.GetMatchingParty(testValue).Should().BeNull();
    }
}