
using System.Security.Cryptography.X509Certificates;

using WarGame.Core;

class Program
{
    /// <summary>
    /// Entry point for the War card game console application.
    /// Handles game mode selection, player setup, event subscriptions,
    /// game execution, and the play-again loop.
    /// </summary>
    static void Main(string[] args)
    {
        //loop allows user to play multiple games if chosen to do so
        while (true)
        {
            //game mode selection
            Console.WriteLine("=== WAR CARD GAME ===");
            Console.WriteLine("Choose game mode:");
            Console.WriteLine("1. Automatic (fast)");
            Console.WriteLine("2. Manual (press ENTER each round)");
            Console.Write("Enter choice: ");

            //manual mode = user presses enter before each round
            string? modeInput = Console.ReadLine();
            bool manualMode = modeInput == "2";
            
            //player count setup
            int playerCount = GetPlayerCount(args);

            // create default player names
            List<string> players = Enumerable.Range(1, playerCount)
                                             .Select(i => $"Player {i}")
                                             .ToList();

            //creates hand for each player
            var playerHands = new PlayerHands();
            foreach (var p in players)
                playerHands.AddPlayer(p);

            //create a new shuffled deck and game engine
            var deck = new Deck();
            var engine = new WarEngine(playerHands);

            
            //printed round updates 
            engine.RoundCompleted += (round, played, tied) =>
            {
                Console.WriteLine($"\nRound {round}:\n");

                //print each players card for the round
                foreach (var kvp in played)
                    Console.WriteLine($"  {kvp.Key} plays {kvp.Value}");

                //print winner or tie
                if (tied.Count == 1)
                    Console.WriteLine($"  Winner: {tied[0]}\n");
                else
                    Console.WriteLine($"\n  Tie between: {string.Join(", ", tied)}\n");

                //print card counts after the round
                PrintCardCounts(playerHands);
            };

            engine.TieOccurred += (round, tied) =>
            {
                Console.WriteLine($"\n  -> Tie detected in round {round} between: {string.Join(", ", tied)}");
            };

            engine.TiebreakerOccurred += (round, tied) =>
            {
                Console.WriteLine($"  -> Tiebreaker round {round} for: {string.Join(", ", tied)}");
            };

            engine.PlayerEliminated += player =>
            {
                Console.WriteLine($"  !! {player} has been eliminated.");
            };

            //deal cards
            engine.Deal(deck, players);

            Console.WriteLine($"\nStarting game with {playerCount} players...");
            PrintCardCounts(playerHands);

            //choose between automatic or manual mode
            var result = manualMode
        ? engine.PlayGameInteractive()
        : engine.PlayGame();

            Console.WriteLine("\n=== GAME OVER ===");

            if (result.Winner != null)
            {
                Console.WriteLine($"Winner: {result.Winner}");
                Console.WriteLine($"Rounds played: {result.Rounds}");
            }
            else
            {
                Console.WriteLine("Result: DRAW");
                Console.WriteLine($"Round limit ({result.Rounds}) reached.");
            }

            Console.WriteLine("\nFinal card counts:");
            PrintCardCounts(playerHands);

            //play again option
            Console.Write("\nPlay again? (y/n): ");
            string? again = Console.ReadLine()?.Trim().ToLower();

            //exits the loop if user inputs "n"
            if (again != "y")
            {
                break;
            }

            //clears for a clean restart
            Console.Clear();
        }
    }

    /// <summary>
    /// Determines the number of players.
    /// First checks command-line arguments; if invalid or missing,
    /// prompts the user until a valid number (2–4) is entered.
    /// </summary>
    static int GetPlayerCount(string[] args)
    {
        //checks command line argument first
        if (args.Length > 0 && int.TryParse(args[0], out int n))
            if (n >= 2 && n <= 4)
                return n;

        //otherwise, prompt the user
        while (true)
        {
            Console.Write("Enter number of players (2–4): ");
            string? input = Console.ReadLine();

            if (int.TryParse(input, out int count) &&
                count >= 2 && count <= 4)
                return count;

            Console.WriteLine("Invalid input. Try again.");
        }
    }

    /// <summary>
    /// Prints the number of cards each player currently has.
    /// Used after each round and at the end of the game.
    /// </summary>
    static void PrintCardCounts(PlayerHands hands)
    {
        foreach (var kvp in hands.Hands)
            Console.WriteLine($"{kvp.Key}: {kvp.Value.Count} cards");
    }
}