namespace Backend.Models;

public enum HandType
{
    OnePair = 1,
    TwoPair = 2,
    ThreeOfAKind = 3,
    FullHouse = 4,
    FourOfAKind = 5,
    FiveOfAKind = 6
}

public sealed class HandStrength : IComparable<HandStrength>
{
    public HandType Type { get; }
    public Rank[] PrimaryRanks { get; }
    public (Rank Rank, Suit Suit)[] Kickers { get; }
    public string Description { get; }

    public HandStrength(HandType type, Rank[] primaryRanks, (Rank, Suit)[] kickers, string description)
    {
        Type = type;
        PrimaryRanks = primaryRanks;
        Kickers = kickers;
        Description = description;
    }

    public int CompareTo(HandStrength? other)
    {
        if (other == null) return 1;

        // Compare hand type first
        var typeComparison = Type.CompareTo(other.Type);
        if (typeComparison != 0) return typeComparison;

        // Compare primary ranks
        for (int i = 0; i < Math.Min(PrimaryRanks.Length, other.PrimaryRanks.Length); i++)
        {
            var rankComparison = PrimaryRanks[i].CompareTo(other.PrimaryRanks[i]);
            if (rankComparison != 0) return rankComparison;
        }

        // Compare kickers
        for (int i = 0; i < Math.Min(Kickers.Length, other.Kickers.Length); i++)
        {
            var kickerRankComparison = Kickers[i].Rank.CompareTo(other.Kickers[i].Rank);
            if (kickerRankComparison != 0) return kickerRankComparison;

            // If ranks are equal, compare suits (higher suit wins)
            var kickerSuitComparison = Kickers[i].Suit.CompareTo(other.Kickers[i].Suit);
            if (kickerSuitComparison != 0) return kickerSuitComparison;
        }

        return 0; // Hands are equal
    }

    public override string ToString() => Description;
}

public static class HandEvaluator
{
    public static HandStrength EvaluateHand(List<Card> cards)
    {
        if (cards.Count != 5)
            throw new ArgumentException("Hand must contain exactly 5 cards", nameof(cards));

        // Count regular cards by rank
        var rankCounts = new Dictionary<Rank, List<Card>>();
        var jokerCount = 0;

        foreach (var card in cards)
        {
            if (card.IsJoker)
            {
                jokerCount++;
            }
            else
            {
                var rank = card.Rank!.Value;
                if (!rankCounts.ContainsKey(rank))
                    rankCounts[rank] = new List<Card>();
                rankCounts[rank].Add(card);
            }
        }

        // Try each hand type from highest to lowest
        var fiveOfAKind = TryFiveOfAKind(rankCounts, jokerCount);
        if (fiveOfAKind != null) return fiveOfAKind;

        var fourOfAKind = TryFourOfAKind(rankCounts, jokerCount);
        if (fourOfAKind != null) return fourOfAKind;

        var fullHouse = TryFullHouse(rankCounts, jokerCount);
        if (fullHouse != null) return fullHouse;

        var threeOfAKind = TryThreeOfAKind(rankCounts, jokerCount);
        if (threeOfAKind != null) return threeOfAKind;

        var twoPair = TryTwoPair(rankCounts, jokerCount);
        if (twoPair != null) return twoPair;

        // Guaranteed to have at least one pair (pigeonhole principle: 5 cards, 4 ranks)
        return TryOnePair(rankCounts, jokerCount);
    }

    private static HandStrength? TryFiveOfAKind(Dictionary<Rank, List<Card>> rankCounts, int jokerCount)
    {
        // Need 4 of a rank + 1 joker
        foreach (var kvp in rankCounts.Where(x => x.Value.Count == 4))
        {
            if (jokerCount >= 1)
            {
                var rank = kvp.Key;
                var description = $"Five {rank}s";
                return new HandStrength(HandType.FiveOfAKind, [rank], [], description);
            }
        }

        return null;
    }

    private static HandStrength? TryFourOfAKind(Dictionary<Rank, List<Card>> rankCounts, int jokerCount)
    {
        // Try 4 natural + kicker
        foreach (var kvp in rankCounts.Where(x => x.Value.Count == 4))
        {
            var quadRank = kvp.Key;
            var kicker = GetBestKicker(rankCounts, jokerCount, [quadRank]);
            var description = $"Four {quadRank}s";
            return new HandStrength(HandType.FourOfAKind, [quadRank], [kicker], description);
        }

        // Try 3 natural + 1 joker + kicker
        foreach (var kvp in rankCounts.Where(x => x.Value.Count == 3))
        {
            if (jokerCount >= 1)
            {
                var quadRank = kvp.Key;
                var kicker = GetBestKicker(rankCounts, jokerCount - 1, [quadRank]);
                var description = $"Four {quadRank}s";
                return new HandStrength(HandType.FourOfAKind, [quadRank], [kicker], description);
            }
        }

        return null;
    }

    private static HandStrength? TryFullHouse(Dictionary<Rank, List<Card>> rankCounts, int jokerCount)
    {
        var tripsRanks = rankCounts.Where(x => x.Value.Count == 3).Select(x => x.Key).ToList();
        var pairRanks = rankCounts.Where(x => x.Value.Count == 2).Select(x => x.Key).ToList();

        // Scenario 1: Natural trips + natural pair
        if (tripsRanks.Count == 1 && pairRanks.Count == 1)
        {
            var tripRank = tripsRanks.Max();
            var pairRank = pairRanks.Max();
            var description = $"{tripRank}s full of {pairRank}s";
            return new HandStrength(HandType.FullHouse, [tripRank, pairRank], [], description);
        }

        // Scenario 2: Two pairs + joker → trips (A♠A♥ K♠K♥ Joker → Aces full of Kings)
        if (pairRanks.Count == 2 && jokerCount >= 1)
        {
            var highPair = pairRanks.Max();
            var lowPair = pairRanks.Min();
            var description = $"{highPair}s full of {lowPair}s";
            return new HandStrength(HandType.FullHouse, [highPair, lowPair], [], description);
        }


        return null;
    }

    private static HandStrength? TryThreeOfAKind(Dictionary<Rank, List<Card>> rankCounts, int jokerCount)
    {
        // Only possible scenario: pair + joker
        foreach (var kvp in rankCounts.Where(x => x.Value.Count == 2))
        {
            if (jokerCount >= 1)
            {
                var tripRank = kvp.Key;
                var kickers = GetBestKickers(rankCounts, jokerCount - 1, [tripRank], 2);
                var description = $"Three {tripRank}s";
                return new HandStrength(HandType.ThreeOfAKind, [tripRank], kickers, description);
            }
        }

        return null;
    }

    private static HandStrength? TryTwoPair(Dictionary<Rank, List<Card>> rankCounts, int jokerCount)
    {
        var pairRanks = rankCounts.Where(x => x.Value.Count == 2).Select(x => x.Key).OrderByDescending(x => x).ToList();

        // Natural two pair
        if (pairRanks.Count >= 2)
        {
            var highPair = pairRanks[0];
            var lowPair = pairRanks[1];
            var kicker = GetBestKicker(rankCounts, jokerCount, [highPair, lowPair]);
            var description = $"{highPair}s and {lowPair}s";
            return new HandStrength(HandType.TwoPair, [highPair, lowPair], [kicker], description);
        }

        // One natural pair + joker → need to make second pair from remaining singles
        if (pairRanks.Count == 1 && jokerCount >= 1)
        {
            var existingPair = pairRanks[0];
            var singleRanks = rankCounts.Where(x => x.Value.Count == 1).Select(x => x.Key).ToList();
            
            if (singleRanks.Count >= 2)
            {
                // Use joker to make a pair with the highest single rank
                var jokerPairRank = singleRanks.Max();
                var highPair = existingPair > jokerPairRank ? existingPair : jokerPairRank;
                var lowPair = existingPair < jokerPairRank ? existingPair : jokerPairRank;
                
                // Remaining single becomes kicker
                var kickerRanks = singleRanks.Where(r => r != jokerPairRank).ToList();
                var kicker = GetBestKicker(rankCounts, 0, [existingPair, jokerPairRank]);
                
                var description = $"{highPair}s and {lowPair}s";
                return new HandStrength(HandType.TwoPair, [highPair, lowPair], [kicker], description);
            }
        }

        return null;
    }

    private static HandStrength TryOnePair(Dictionary<Rank, List<Card>> rankCounts, int jokerCount)
    {
        // Natural pair (guaranteed to exist due to pigeonhole principle)
        var naturalPair = rankCounts.Where(x => x.Value.Count == 2).OrderByDescending(x => x.Key).FirstOrDefault();
        if (naturalPair.Key != default)
        {
            var pairRank = naturalPair.Key;
            var kickers = GetBestKickers(rankCounts, jokerCount, [pairRank], 3);
            var description = $"Pair of {pairRank}s";
            return new HandStrength(HandType.OnePair, [pairRank], kickers, description);
        }

        // Fallback: Use joker to create pair (this should rarely happen)
        var bestPairRank = GetBestRankForJoker(rankCounts, []);
        var jokerKickers = GetBestKickers(rankCounts, jokerCount - 1, [bestPairRank], 3);
        var jokerDescription = $"Pair of {bestPairRank}s";
        return new HandStrength(HandType.OnePair, [bestPairRank], jokerKickers, jokerDescription);
    }

    private static (Rank Rank, Suit Suit) GetBestKicker(Dictionary<Rank, List<Card>> rankCounts, int availableJokers, Rank[] excludeRanks)
    {
        // Try natural cards first
        var availableCards = rankCounts
            .Where(kvp => !excludeRanks.Contains(kvp.Key))
            .SelectMany(kvp => kvp.Value)
            .OrderByDescending(c => c.Rank)
            .ThenByDescending(c => c.Suit)
            .ToList();

        if (availableCards.Any())
        {
            var bestCard = availableCards.First();
            return (bestCard.Rank!.Value, bestCard.Suit!.Value);
        }

        // Use joker if available
        if (availableJokers > 0)
        {
            var bestRank = GetBestRankForJoker(rankCounts, excludeRanks);
            return (bestRank, Suit.Spades); // Joker adopts highest suit
        }

        throw new InvalidOperationException("No kicker available");
    }

    private static (Rank Rank, Suit Suit)[] GetBestKickers(Dictionary<Rank, List<Card>> rankCounts, int availableJokers, Rank[] excludeRanks, int count)
    {
        var kickers = new List<(Rank Rank, Suit Suit)>();
        var usedJokers = 0;

        // Get natural cards first
        var availableCards = rankCounts
            .Where(kvp => !excludeRanks.Contains(kvp.Key))
            .SelectMany(kvp => kvp.Value)
            .OrderByDescending(c => c.Rank)
            .ThenByDescending(c => c.Suit)
            .ToList();

        foreach (var card in availableCards.Take(count))
        {
            kickers.Add((card.Rank!.Value, card.Suit!.Value));
        }

        // Fill remaining with jokers
        while (kickers.Count < count && usedJokers < availableJokers)
        {
            var usedRanks = excludeRanks.Concat(kickers.Select(k => k.Rank)).ToArray();
            var bestRank = GetBestRankForJoker(rankCounts, usedRanks);
            kickers.Add((bestRank, Suit.Spades)); // Joker adopts highest suit
            usedJokers++;
        }

        return kickers.ToArray();
    }

    private static Rank GetBestRankForJoker(Dictionary<Rank, List<Card>> rankCounts, Rank[] excludeRanks)
    {
        // Return highest rank not in excludeRanks
        var allRanks = Enum.GetValues<Rank>().OrderByDescending(r => r);
        return allRanks.First(r => !excludeRanks.Contains(r));
    }
}
