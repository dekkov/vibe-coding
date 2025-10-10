using System.Security.Cryptography;

namespace Backend.Models;

public sealed class Deck
{
    private readonly List<Card> _cards;
    private int _nextCardIndex;

    public Deck()
    {
        _cards = CreateStandardDeck();
        _nextCardIndex = 0;
        Shuffle();
    }

    public int CountRemaining => _cards.Count - _nextCardIndex;

    private static List<Card> CreateStandardDeck()
    {
        var cards = new List<Card>();
        
        // Add 4 of each rank (A, K, Q, J) - one per suit
        foreach (Rank rank in Enum.GetValues<Rank>())
        {
            foreach (Suit suit in Enum.GetValues<Suit>())
            {
                cards.Add(new Card(rank, suit));
            }
        }
        
        // Add one Joker
        cards.Add(Card.CreateJoker());
        
        return cards;
    }

    public void Shuffle()
    {
        // Fisher-Yates shuffle using cryptographically secure random
        using var rng = RandomNumberGenerator.Create();
        
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            var randomBytes = new byte[4];
            rng.GetBytes(randomBytes);
            int j = (int)(BitConverter.ToUInt32(randomBytes, 0) % (i + 1));
            
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
        
        _nextCardIndex = 0;
    }

    public List<Card> Draw(int count)
    {
        if (count < 0)
            throw new ArgumentException("Cannot draw negative number of cards", nameof(count));
            
        if (count > CountRemaining)
            throw new InvalidOperationException($"Cannot draw {count} cards, only {CountRemaining} remaining");

        var drawnCards = new List<Card>();
        for (int i = 0; i < count; i++)
        {
            drawnCards.Add(_cards[_nextCardIndex++]);
        }
        
        return drawnCards;
    }

    public Card DrawOne()
    {
        var cards = Draw(1);
        return cards[0];
    }

    public void Reset()
    {
        _nextCardIndex = 0;
        Shuffle();
    }
}
