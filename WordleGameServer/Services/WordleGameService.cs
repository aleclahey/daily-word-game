/*
 * Program:         Project2_Wordle
 * File:            DailyWordleService.cs
 * Date:            March 13, 2025
 * Author:          Jazz Leibur, 0690432, Section 2 & Alec Lahey, 1056146, Section 2
 * Description:     This module implements the rpc's of the DailyWordleService service contract. This represents a game play of Wordle.
 *                  Updated to use WCF client for DailyWord service.
 */

using Grpc.Core;
using WordleGameServer.Protos;
using WordServer.Services;
using Newtonsoft.Json;

namespace WordleGameServer.Services
{
    public class DailyWordleService : DailyWordle.DailyWordleBase
    {
        private IDailyWordService _dailyWordClient;
        private string? _statsFilePath;
        private const int MAX_GUESSES = 6;

        // Dictionary to track game state for each player session
        // There may be more than one session since multiple players can play at once
        private readonly Dictionary<string, GameSession> _sessions = new Dictionary<string, GameSession>();

        // Constructor to instantiate some private variables
        public DailyWordleService(IDailyWordService dailyWordClient)
        {
            _dailyWordClient = dailyWordClient;
            _statsFilePath = Path.Combine(Directory.GetCurrentDirectory(), @"..\Data\user_stats.json");

            // Make sure the directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(_statsFilePath));
        }

        // Implementation of Play rpc that will run the game
        public override async Task Play(IAsyncStreamReader<WordGuess> request, IServerStreamWriter<WordGuessResponse> responseStream, ServerCallContext context)
        {
            string clientId = context.Peer; // used to keep track of the session

            // create session
            if (!_sessions.TryGetValue(clientId, out GameSession? session))
            {
                // Get the daily word from the WordServer using WCF (synchronous call)
                var wordResponse = _dailyWordClient.GetWord(new GetWordRequest());
                string wordToGuess = wordResponse.Word.ToLower();
                Console.WriteLine(wordToGuess);

                // Create a game session (private class)
                session = new GameSession
                {
                    date = DateTime.Today,
                    WordToGuess = wordToGuess,
                    GuessCount = 0,
                    GameOver = false,
                    IsCorrect = false,
                    UsedLetters = new HashSet<char>(),
                    UnusedLetters = new HashSet<char>("abcdefghijklmnopqrstuvwxyz")
                };

                _sessions[clientId] = session;
            }

            // Process each guess from the stream
            await foreach (var wordGuess in request.ReadAllAsync())
            {
                string guess = wordGuess.Word.ToLower();

                // Check if the guess is already correct
                if (session.IsCorrect)
                {
                    session.GameOver = true;
                }

                // Check if game is already over and exit loop
                if (session.GameOver)
                {
                    await responseStream.WriteAsync(new WordGuessResponse
                    {
                        Word = guess,
                        IsCorrect = session.IsCorrect,
                        GameOver = true,
                        CorrectPosition = "",
                        Included = string.Join(",", session.UsedLetters.OrderBy(c => c)),  // Join with commas
                        Incorrect = string.Join(",", session.UnusedLetters.OrderBy(c => c)), // Join with commas
                        NotUsed = string.Join(",", session.UnusedLetters),
                    });

                    return;
                }

                // Check if guess is valid with dailyword server using WCF (synchronous call)
                var validationResponse = _dailyWordClient.ValidateWord(new SubmittedWord { Word = guess });

                // if guess is not a valid word go to next request
                if (!validationResponse.IsValid)
                {
                    await responseStream.WriteAsync(new WordGuessResponse
                    {
                        Word = guess,
                        IsCorrect = false,
                        GameOver = false,
                        CorrectPosition = "",
                        Included = "",
                        Incorrect = "",
                        NotUsed = string.Join(",", session.UnusedLetters),
                        ValidGuess = false
                    });

                    continue; // will not be counted as a GuessCount
                }

                // Count this as a valid guess
                session.GuessCount++;

                if (session.GuessCount == MAX_GUESSES || session.IsCorrect)
                {
                    session.GameOver = true;
                }

                char[] results = new char[5] { ' ', ' ', ' ', ' ', ' ' };

                // Holds alphabet matches and quantity for each letter
                Dictionary<char, int> matches = new Dictionary<char, int>
                {
                    {'a', 0}, {'b', 0}, {'c', 0}, {'d', 0}, {'e', 0}, {'f', 0}, {'g', 0}, {'h', 0},
                    {'i', 0}, {'j', 0}, {'k', 0}, {'l', 0}, {'m', 0}, {'n', 0}, {'o', 0}, {'p', 0},
                    {'q', 0}, {'r', 0}, {'s', 0}, {'t', 0}, {'u', 0}, {'v', 0}, {'w', 0}, {'x', 0},
                    {'y', 0}, {'z', 0}
                };

                // Using hashset so there are no duplicate letters being outputted
                HashSet<char> incorrectLetters = new HashSet<char>();
                HashSet<char> includedLetters = new HashSet<char>();

                // Find correct letters in correct positions
                for (int i = 0; i < guess.Length; i++)
                {
                    if (guess[i] == session.WordToGuess[i])
                    {
                        results[i] = '*';
                        matches[guess[i]]++;
                        includedLetters.Add(guess[i]);
                    }
                }

                // Find correct letters in wrong positions or incorrect letters
                for (int i = 0; i < guess.Length; i++)
                {
                    if (results[i] != '*' && session.WordToGuess.Contains(guess[i]))
                    {
                        if (matches[guess[i]] < session.WordToGuess.Count(l => l == guess[i]))
                        {
                            results[i] = '?';
                            matches[guess[i]]++;
                            includedLetters.Add(guess[i]);
                        }
                    }
                    else if (results[i] != '*' && !session.WordToGuess.Contains(guess[i]))
                    {
                        results[i] = 'x';
                        incorrectLetters.Add(guess[i]);
                    }
                }

                // results array to string for return
                // adding commas for letters
                string correctPosition = new string(results);
                string included = string.Join(",", includedLetters);
                string incorrect = string.Join(",", incorrectLetters);

                // Checks if the guess was correct/matches word of the day
                // If true game is over
                bool isCorrect = (guess == session.WordToGuess);
                if (isCorrect)
                {
                    session.GameOver = true;
                    session.IsCorrect = true;
                }

                // Update letters
                foreach (char c in guess)
                {
                    session.UsedLetters.Add(c);
                    session.UnusedLetters.Remove(c);
                }

                // Update game statistics if game is over
                if (session.GameOver)
                {
                    UpdateUserStats(session.date, isCorrect, session.GuessCount);
                }

                // Send the response to the client as a stream
                await responseStream.WriteAsync(new WordGuessResponse
                {
                    Word = guess,
                    IsCorrect = isCorrect,
                    GameOver = session.GameOver,
                    CorrectPosition = correctPosition,
                    Included = included,
                    Incorrect = incorrect,
                    NotUsed = string.Join(",", session.UnusedLetters),
                    ValidGuess = true
                });

                // if the game is over no more responses are needed
                if (session.GameOver)
                {
                    break;
                }
            }

            // Return a generic response if no guesses were processed or the game is over
            await responseStream.WriteAsync(new WordGuessResponse
            {
                Word = "",
                IsCorrect = false,
                GameOver = false,
                CorrectPosition = "",
                Included = "",
                Incorrect = "",
                NotUsed = string.Join(",", session.UnusedLetters),
                ValidGuess = false
            });
        }

        // Implementation of GetStats to return UserStatistics data
        public override Task<UserStatistics> GetStats(User request, ServerCallContext context)
        {
            var stats = ReadUserStats();

            double averageGuess = 0;
            if (stats.Winners > 0)
            {
                // Only calculates average for winners
                averageGuess = (double)stats.TotalGuesses / stats.Winners;
            }

            stats.AverageGuess = averageGuess;

            return Task.FromResult(new UserStatistics
            {
                Players = stats.Players,
                Winners = stats.Winners,
                AverageGuess = stats.AverageGuess
            });
        }

        // Helper method to update user statistics
        // Using Mutex object here to prevent locking
        private void UpdateUserStats(DateTime sessionDate, bool isCorrect, int guessCount)
        {
            bool acquiredLock = false;
            Mutex statsMutex = new Mutex();
            try
            {
                // Checking if its a new day
                // This helper method will reset the statistics if it is a new day
                VerifyStats();

                // Acquire mutex to prevent concurrent file access
                acquiredLock = statsMutex.WaitOne(10000);
                if (!acquiredLock)
                {
                    throw new TimeoutException("Failed to acquire mutex for updating user stats.");
                }

                // Get current stats
                var stats = ReadUserStats();

                // Reset stats for the current day if the session's date has changed
                if (sessionDate != DateTime.Today)
                {
                    stats = new UserStatistics
                    {
                        Players = 0,
                        Winners = 0,
                        TotalGuesses = 0,
                        AverageGuess = 0.0,
                        CurrentDay = DateTime.Today.Date.ToString()
                    };
                }

                // Update stats if the game was won
                if (isCorrect)
                {
                    stats.Winners++;
                    stats.TotalGuesses += guessCount;
                    stats.AverageGuess = (double)stats.TotalGuesses / stats.Winners;
                    stats.CurrentDay = DateTime.Today.Date.ToString();
                }

                stats.CurrentDay = DateTime.Today.Date.ToString();
                stats.Players++;

                // save updates
                SaveUserStats(stats);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (acquiredLock)
                {
                    statsMutex.ReleaseMutex();
                }
            }
        }

        // Helper method to read user stats
        private UserStatistics ReadUserStats()
        {
            if (!File.Exists(_statsFilePath))
            {
                return new UserStatistics
                {
                    Players = 0,
                    Winners = 0,
                    TotalGuesses = 0,
                    AverageGuess = 0.0,
                    CurrentDay = DateTime.Today.Date.ToString()
                };
            }

            try
            {
                string json = File.ReadAllText(_statsFilePath);
                var stats = JsonConvert.DeserializeObject<UserStatistics>(json);
                return stats;
            }
            catch (Exception)
            {
                return new UserStatistics
                {
                    Players = 0,
                    Winners = 0,
                    TotalGuesses = 0,
                    AverageGuess = 0.0,
                    CurrentDay = DateTime.Today.Date.ToString()
                };
            }
        }

        // Checks if date has changed
        private void VerifyStats()
        {
            var stats = ReadUserStats();
            DateTime today = DateTime.Today.Date;

            // If date has changed, reset stats
            if (today.ToString() != stats.CurrentDay)
            {
                // Reset the stats
                var resetStats = new UserStatistics
                {
                    Players = 0,
                    Winners = 0,
                    TotalGuesses = 0,
                    AverageGuess = 0.0
                };

                // Save the reset stats
                SaveUserStats(resetStats);
            }
        }

        // Helper method to save user stats
        private void SaveUserStats(UserStatistics stats)
        {
            string json = JsonConvert.SerializeObject(stats);
            File.WriteAllText(_statsFilePath, json);
        }

        // private class to track game session for a player
        private class GameSession
        {
            public DateTime date { get; set; }
            public string WordToGuess { get; set; }
            public int GuessCount { get; set; }
            public bool GameOver { get; set; }
            public bool IsCorrect { get; set; }
            public HashSet<char> UsedLetters { get; set; }
            public HashSet<char> UnusedLetters { get; set; }
        }
    }
}