namespace Backend.Models;

public enum Suit
{
    Clubs = 1,
    Diamonds = 2,
    Hearts = 3,
    Spades = 4
}

public enum Rank
{
    Jack = 1,
    Queen = 2,
    King = 3,
    Ace = 4
}

public sealed class Card
{
    public Rank? Rank { get; }
    public Suit? Suit { get; }
    public bool IsJoker { get; }

    // Regular card constructor
    public Card(Rank rank, Suit suit)
    {
        Rank = rank;
        Suit = suit;
        IsJoker = false;
    }

    // Joker constructor
    private Card()
    {
        Rank = null;
        Suit = null;
        IsJoker = true;
    }

    public static Card CreateJoker() => new();

    public override string ToString()
    {
        if (IsJoker) return "Joker";
        return $"{Rank}{GetSuitSymbol(Suit!.Value)}";
    }

    private static string GetSuitSymbol(Suit suit) => suit switch
    {
        Models.Suit.Spades => "♠",
        Models.Suit.Hearts => "♥",
        Models.Suit.Diamonds => "♦",
        Models.Suit.Clubs => "♣",
        _ => throw new ArgumentOutOfRangeException(nameof(suit))
    };

    public override bool Equals(object? obj)
    {
        if (obj is not Card other) return false;
        return IsJoker == other.IsJoker && Rank == other.Rank && Suit == other.Suit;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Rank, Suit, IsJoker);
    }
}
