using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SlopChat
{
    public static class ProfanityFilter
    {
        public const string CensoredMessage = "I said a naughty word!";
        public const string ProfanityError = "Your message contains banned words.";

        private static List<string> BannedWords = LoadBannedWords();

        private static List<string> LoadBannedWords()
        {
            var result = new List<string>();

            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SlopChat.res.profanity.txt");
            if (stream is null) throw new Exception("Could not load profanity filter");
            var text = new StreamReader(stream).ReadToEnd();

            var lines = text.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("#")) continue;
                trimmed = RemoveSpecialCharacters(trimmed);
                trimmed = SpecialCharactersToLetters(trimmed);
                result.Add(trimmed);
            }

            return result;
        }

        public static bool ContainsProfanity(string text)
        {
            foreach (var line in BannedWords)
            {
                if (text.ToLower().Contains(line.ToLower())) return true;
            }

            return false;
        }

        public static bool TMPContainsProfanity(string tmpText)
        {
            var removedTags = TMPFilter.RemoveAllTags(tmpText);
            removedTags = SpecialCharactersToLetters(removedTags);
            removedTags = RemoveSpecialCharacters(removedTags);

            var withTags = tmpText;
            withTags = SpecialCharactersToLetters(withTags);
            withTags = RemoveSpecialCharacters(withTags);

            var withTagsNoRepeats = RemoveRepeatedLetters(withTags);
            var removedTagsNoRepeats = RemoveRepeatedLetters(removedTags);

            if (ContainsProfanity(withTags) || ContainsProfanity(removedTags) || ContainsProfanity(withTagsNoRepeats) || ContainsProfanity(removedTagsNoRepeats))
                return true;
            return false;
        }

        public static string RemoveRepeatedLetters(string text)
        {
            var final = "";
            var lastChar = char.MinValue;
            foreach(var c in text)
            {
                if (c != lastChar)
                {
                    lastChar = c;
                    final += c;
                }
            }
            UnityEngine.Debug.Log(final);
            return final;
        }

        public static string RemoveSpecialCharacters(string text)
        {
            var reg = new Regex(@"[^A-Za-z0-9]");
            text = reg.Replace(text, string.Empty);
            return text;
        }

        public static string SpecialCharactersToLetters(string text)
        {
            text = text.Replace("0", "o");
            text = text.Replace("1", "i");
            text = text.Replace("3", "e");
            text = text.Replace("4", "a");
            text = text.Replace("5", "s");
            text = text.Replace("6", "g");
            text = text.Replace("7", "t");
            text = text.Replace("0", "o");

            text = text.Replace("@", "a");
            return text;
        }
    }
}
