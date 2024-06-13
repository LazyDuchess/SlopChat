using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

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
            var validText = RemoveInvalidCharacters(tmpText);
            if (ContainsProfanity(validText) || ContainsProfanity(TMPFilter.RemoveAllTags(validText)))
                return true;
            return false;
        }

        public static string RemoveInvalidCharacters(string text)
        {
            text = text.Replace("​", string.Empty);
            return text;
        }
    }
}
