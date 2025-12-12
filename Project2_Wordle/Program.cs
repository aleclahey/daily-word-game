/*
 * Program:         Project2_Wordle
 * File:            Program.cs
 * Date:            March 13, 2025
 * Author:          Jazz Leibur, 0690432, Section 2 & Alec Lahey, 1056146, Section 2
 * Description:     Client implementation of WordleGameService. DailyWordService is used within the WordleGameService and is not exposed to the client.
 */

using Grpc.Net.Client;
using WordleGameServer.Protos;
using Grpc.Core;

namespace Project2_Wordle
{
    public class Program
    {
        
        static async Task Main(string[] args)
        {
            PrintMenu();
            await PlayGameAsync();
        }

        //Method to run Play rpc
        private static async Task PlayGameAsync()
        {
            try
            {
                var channel = GrpcChannel.ForAddress("https://localhost:7245");
                var game = new DailyWordle.DailyWordleClient(channel);

                

                int guesses = 1;
                bool gameOver = false;

                bool winner = false;

                using (var play = game.Play())
                {
                    // Loop to alternate between user guesses and server responses
                    while (!gameOver)
                    {
                        string guess = "";


                        // Prompt user for a guess, validates length
                        while (guess.Length != 5)
                        {

                            Console.Write($"\n({guesses}):\t");
                            guess = Console.ReadLine()?.Trim() ?? "";
                            if (guess.Length != 5)
                                Console.WriteLine("\nWord must be 5 letters!\n");
                        }

                        // Send the guess to the server
                        var guessRequest = new WordGuess { Word = guess };
                        await play.RequestStream.WriteAsync(guessRequest);


                        if (gameOver)
                            break;

                        // Task to handle server responses
                        var responseTask = Task.Run(async () =>
                        {
                            while (await play.ResponseStream.MoveNext())
                            {
                                WordGuessResponse response = play.ResponseStream.Current;

                                winner = response.IsCorrect;

                                if (response.GameOver)
                                {
                                    gameOver = true;
                                    break;
                                }

                                if (response.ValidGuess)
                                {
                                    Console.WriteLine($"\t{response.CorrectPosition}");
                                    Console.WriteLine($"\nIncluded: {response.Included}");
                                    Console.WriteLine($"Not Used: {response.NotUsed}");
                                    Console.WriteLine($"Incorrect: {response.Incorrect}");
                                    guesses++;

                                }
                                else
                                {
                                    Console.WriteLine("\n" + response.Word + " is not a valid word!\n");
                                }



                            }
                        });

                        //Delay to give response time before calling another request
                        await Task.Delay(500);
                    }


                   

                }

                // Complete the request
                //await play.RequestStream.CompleteAsync();

                if(winner)
                    Console.WriteLine("\nYou Win! Thanks for playing!");
                else
                    Console.WriteLine("\nYou Lost :( Thanks for playing!");

                Console.WriteLine("\nCome back tomorrow for a new word!");

                // Get and display statistics
                var statsResponse = await game.GetStatsAsync(new User());

                Console.WriteLine("\nGame Statistics:\n");
                Console.WriteLine($"Total Players: {statsResponse.Players}");
                Console.WriteLine($"Winners: {statsResponse.Winners}");
                Console.WriteLine("Average Guess: {0:N2}",statsResponse.AverageGuess);

            }
            catch (RpcException)
            {
                Console.WriteLine("\nERROR: The wordled service is not currently available.");
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: "+ e);
            }
        }

        //Helper method to print menu
        private static void PrintMenu()
        {
            Console.WriteLine("+-------------------+");
            Console.WriteLine("|   W O R D L E D   |");
            Console.WriteLine("+-------------------+");
            Console.WriteLine("Created by Jazz Leibur and Alec Lahey\n\n");
            Console.WriteLine("\nYou have 6 chances to guess a 5-letter word.");
            Console.WriteLine("Each guess must be a 'playable' 5 letter word.");
            Console.WriteLine("After a guess the game will display a series of characters to show you how good your guess was.");
            Console.WriteLine("x - means the letter above is not in the word.");
            Console.WriteLine("? - means the letter should be in another spot.");
            Console.WriteLine("* - means the letter is correct in this spot.\n");

            //Will always start will full alphabet so this can be hardcoded
            Console.WriteLine("\n\tAvailable: a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z");

        }
    }
}
