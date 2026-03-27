using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design;
using System.Runtime.CompilerServices;
using WarGame.Core;

namespace WarGame.Core;

// suits available in a deck of cards
public enum Suit
{
    Clubs,
    Diamonds,
    Hearts,
    Spades
}
//ranks available in a deck of cards. (ace is 14 not 1)
public enum Rank
{
    Two = 2,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Ten,
    Jack,
    Queen,
    King,
    Ace
}


/// <summary>
/// Represents a single playing card with Suit and Rank.
/// Cards are comparable by Rank only (Ace high).
/// </summary>
public class Card : IComparable<Card>
{
    public Suit Suit { get; }
    public Rank Rank { get; }

    public Card(Suit suit, Rank rank)
    {
        Suit = suit;
        Rank = rank;
    }

    //compares this card to another card by rank only
    public int CompareTo(Card? other)
    {
        if (other == null) return 1;
        return Rank.CompareTo(other.Rank);
    }

    public override string ToString()
    {
        return $"{Rank} of {Suit}";
    }
}

//shuffles itself & represents the full 52 card deck stored in a stack. 

public class Deck
{
    private readonly Stack<Card> _cards = new();

    //num of cards remaining in deck
    public int Count => _cards.Count;

    public Deck()
    {
        Initialize();
        Shuffle();
    }
    //creates all cards in rank/suit order
    private void Initialize()
    {
        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            foreach (Rank rank in Enum.GetValues(typeof(Rank)))
            {
                _cards.Push(new Card(suit, rank));
            }
        }
    }
    /// <summary>
    /// Performs a shuffle on the deck.
    /// Converts the stack to a list, shuffles, then rebuilds the stack.
    /// </summary>
    private void Shuffle()
    {
        var rng = new Random();
        var list = new List<Card>(_cards);
        _cards.Clear();

        
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        foreach (var card in list)
            _cards.Push(card);
    }
    /// <summary>
    /// Draws the top card from the deck.
    /// </summary>
    public Card Draw()
    {
        return _cards.Pop();
    }
}


/// <summary>
/// Represents a player's hand using a FIFO queue.
/// </summary>
public class Hand
{
    private readonly Queue<Card> _cards = new();

    public int Count => _cards.Count;
    public bool HasCards => _cards.Count > 0;
    /// <summary>
    /// Removes and returns the next card from the front of the hand.
    /// </summary>
    public Card Draw() => _cards.Dequeue();
    // <summary>
    /// Adds cards to the back of the hand (used when winning the pot).
    /// </summary>
    public void AddToBack(IEnumerable<Card> cards)
    {
        foreach (var card in cards)
            _cards.Enqueue(card);
    }
}

/// <summary>
/// Holds all players' hands, keyed by player name.
/// </summary>
public class PlayerHands
{
    public Dictionary<string, Hand> Hands { get; } = new();
    //creates a new hand for a player
    public void AddPlayer(string name)
    {
        Hands[name] = new Hand();
    }
    //returns all players who still have cards
    public IEnumerable<string> ActivePlayers =>
        Hands.Keys.Where(p => Hands[p].HasCards);
}


/// <summary>
/// Stores the face up cards played in a round.
/// </summary>
public class PlayedCards
{
    public Dictionary<string, Card> Cards { get; } = new();

    //records the card a player played this round
    public void Add(string player, Card card)
    {
        Cards[player] = card;
    }

    //clears all played cards
    public void Clear() => Cards.Clear();
}

/// <summary>
/// Core game engine for War. Handles dealing, rounds, ties, pot management, and win conditions.
/// </summary>
public class WarEngine
{
    private readonly PlayerHands _playerHands;
    private readonly List<Card> _pot = new();
    private const int RoundLimit = 10000;

    //events for the console UI to use
    public event Action<int, Dictionary<string, Card>, List<string>>? RoundCompleted;
    public event Action<int, List<string>>? TieOccurred;
    public event Action<int, List<string>>? TiebreakerOccurred;
    public event Action<string>? PlayerEliminated;

    private HashSet<string> _previousActive = new();

    public WarEngine(PlayerHands playerHands)
    {
        _playerHands = playerHands;
    }

    /// <summary>
    /// Deals cards in round order. First players get extra cards if uneven.
    /// </summary>
    public void Deal(Deck deck, List<string> players)
    {
        int index = 0;

        while (deck.Count > 0)
        {
            var card = deck.Draw();
            string player = players[index];
            _playerHands.Hands[player].AddToBack(new[] { card });

            index = (index + 1) % players.Count;
        }
    }

    /// <summary>
    /// Runs the full game until a winner or round limit is reached.
    /// </summary>
    

    public GameResult PlayGame()
    {
        int round = 0;
        _previousActive = _playerHands.ActivePlayers.ToHashSet();

        while (round < RoundLimit)
        {
            round++;

            var active = _playerHands.ActivePlayers.ToList();

            // detects eliminations
            foreach (var p in _previousActive.Except(active))
                PlayerEliminated?.Invoke(p);

            _previousActive = active.ToHashSet();
            //if only one player remains, they win
            if (active.Count == 1)
                return new GameResult(active[0], round, false);

            //plays a round, game ends if winner is returned
            var winner = PlayRound(active, round);
            if (winner != null)
                return new GameResult(winner, round, false);
        }

        // Round limit reached — determine winner by card count
        int max = _playerHands.Hands.Max(h => h.Value.Count);
        var leaders = _playerHands.Hands
            .Where(h => h.Value.Count == max)
            .Select(h => h.Key)
            .ToList();

        if (leaders.Count == 1)
            return new GameResult(leaders[0], RoundLimit, true);

        return new GameResult(null, RoundLimit, true); // draw
    }

    /// <summary>
    /// Plays a single round. Returns winner name if game ends.
    /// </summary>
    private string? PlayRound(List<string> players, int roundNumber)
    {
        var played = new PlayedCards();

        // Step 1: Everyone reveals a card
        foreach (var p in players)
        {
            if (_playerHands.Hands[p].HasCards)
            {
                var card = _playerHands.Hands[p].Draw();
                played.Add(p, card);
                _pot.Add(card);
            }
        }

        // Step 2: Determine highest rank
        int maxRank = played.Cards.Max(c => (int)c.Value.Rank);
        var tied = played.Cards
            .Where(c => (int)c.Value.Rank == maxRank)
            .Select(c => c.Key)
            .ToList();

        // notify UI
        RoundCompleted?.Invoke(
            roundNumber,
            new Dictionary<string, Card>(played.Cards),
            tied
        );
        //if one player has a higher card, they win
        if (tied.Count == 1)
        {
            AwardPot(tied[0]);
            return CheckForGameEnd();
        }
        //otherwise, tie must be resolved

        TieOccurred?.Invoke(roundNumber, tied);
        return ResolveTie(tied, roundNumber);
    }

    /// <summary>
    /// Recursively resolves ties until a single winner emerges.
    /// </summary>
    private string? ResolveTie(List<string> tiedPlayers, int roundNumber)
    {
        TiebreakerOccurred?.Invoke(roundNumber, tiedPlayers);

        var survivors = new List<string>();

        //each tied player draws a card
        foreach (var p in tiedPlayers)
        {
            if (_playerHands.Hands[p].HasCards)
            {
                var card = _playerHands.Hands[p].Draw();
                _pot.Add(card);
                survivors.Add(p);
            }
        }

        //if all players have no cards, no winner
        if (survivors.Count == 0)
            return null;

        //if player survives tiebreaker, they win
        if (survivors.Count == 1)
        {
            AwardPot(survivors[0]);
            return CheckForGameEnd();
        }

        //otherwise, continue 
        return ResolveTie(survivors, roundNumber);
    }

    /// <summary>
    /// Gives all cards in the pot to the winning player.
    /// </summary>
    private void AwardPot(string winner)
    {
        _playerHands.Hands[winner].AddToBack(_pot);
        _pot.Clear();
    }

    /// <summary>
    /// Checks if only one player remains with cards.
    /// </summary>
    private string? CheckForGameEnd()
    {
        var active = _playerHands.ActivePlayers.ToList();
        return active.Count == 1 ? active[0] : null;
    }

    /// <summary>
    /// Manual mode version of PlayGame().
    /// Pauses before each round until the user presses ENTER.
    /// </summary>
    public GameResult PlayGameInteractive()
    {
        int round = 0;
        _previousActive = _playerHands.ActivePlayers.ToHashSet();

        while (round < RoundLimit)
        {
            Console.WriteLine("\nPress ENTER to play next round...");
            Console.ReadLine();

            round++;

            var active = _playerHands.ActivePlayers.ToList();

            foreach (var p in _previousActive.Except(active))
                PlayerEliminated?.Invoke(p);

            _previousActive = active.ToHashSet();

            if (active.Count == 1)
                return new GameResult(active[0], round, false);

            var winner = PlayRound(active, round);
            if (winner != null)
                return new GameResult(winner, round, false);
        }

        return DetermineLimitWinner();
    }

    /// <summary>
    /// Determines the winner when the round limit is reached.
    /// Winner is the player with the most cards.
    /// </summary>
    private GameResult DetermineLimitWinner()
    {
        int max = _playerHands.Hands.Max(h => h.Value.Count);
        var leaders = _playerHands.Hands
            .Where(h => h.Value.Count == max)
            .Select(h => h.Key)
            .ToList();

        if (leaders.Count == 1)
            return new GameResult(leaders[0], RoundLimit, true);

        return new GameResult(null, RoundLimit, true);
    }
}



/// <summary>
/// Final result of a War game.
/// </summary>
public record GameResult(string? Winner, int Rounds, bool LimitReached);
