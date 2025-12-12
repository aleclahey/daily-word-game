/*
 * Program:         Project2_Wordle
 * File:            DailyWordService.cs
 * Date:            March 13, 2025
 * Author:          Jazz Leibur, 0690432, Section 2 & Alec Lahey, 1056146, Section 2
 * Description:     WCF implementation of the DailyWord service. 
 */

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace WordServer.Services
{
    public class DailyWordService : IDailyWordService
    {
        private List<string> _words = new List<string>();
        private string _wordFilePath = Path.Combine(Directory.GetCurrentDirectory(), @"..\Data\daily_word.json");

        // Constructor will call the helper method to add words from the file to the words list
        public DailyWordService()
        {
            ReadWordFile();
        }

        // Implementation of the GetWord operation that will return a word from the wordle list based on the calendar day
        public DailyWordResponse GetWord(GetWordRequest request)
        {
            string word = GetWordForToday();
            return new DailyWordResponse { Word = word };
        }

        // Implementation of the ValidateWord operation that will validate if a users input is a valid Wordle word
        public ValidationResponse ValidateWord(SubmittedWord request)
        {
            bool isValid = _words.Contains(request.Word);
            return new ValidationResponse { IsValid = isValid };
        }

        // A helper method to read from the wordle.json file and add it to the words list
        private void ReadWordFile()
        {
            try
            {
                string json = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), @"..\Data\wordle.json"));
                this._words = JsonConvert.DeserializeObject<List<string>>(json);

                // If there are no words, throw an exception for the client to handle as the game cannot be played
                if (this._words == null)
                {
                    throw new Exception();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error getting words from file. Error" + ex.Message);
            }
        }

        // A helper method to get the word for today's calendar day
        private string GetWordForToday()
        {
            try
            {
                string word = "";
                DateTime currentDate = DateTime.Today;

                // If there is an existing word file, check it and see if the date matches today
                if (File.Exists(_wordFilePath))
                {
                    string json = File.ReadAllText(_wordFilePath);
                    var savedData = JsonConvert.DeserializeObject<DailyWordData>(json);

                    if (savedData != null && savedData.Date == currentDate.ToString("yyyy-MM-dd"))
                    {
                        word = savedData.Word;
                    }
                }

                // If there have been no words saved, or the date has changed, get a new word and save it
                if (string.IsNullOrEmpty(word))
                {
                    Random _random = new Random();
                    word = _words[_random.Next(_words.Count)];
                    SaveWordForToday(word);
                }

                return word;
            }
            catch (Exception ex)
            {
                // If there is an issue with the word of the day, throw an exception for the client to handle as the game cannot be played 
                throw new Exception("Issue parsing or deserializing JSON file. Error: " + ex.Message);
            }
        }

        // A helper method to save today's word to the file
        private void SaveWordForToday(string word)
        {
            try
            {
                var wordData = new DailyWordData
                {
                    Date = DateTime.Today.ToString("yyyy-MM-dd"),
                    Word = word
                };

                File.WriteAllText(_wordFilePath, JsonConvert.SerializeObject(wordData));
            }
            catch (Exception ex)
            {
                // If there is an issue with the word of the day, throw an exception for the client to handle as the game cannot be played 
                throw new Exception("Issue parsing or serializing to JSON file. Error: " + ex.Message);
            }
        }

        // A class to represent the saved word data so that it can be serialized to, and deserialized from a json file
        public class DailyWordData
        {
            public string Date { get; set; }
            public string Word { get; set; }
        }
    }
}