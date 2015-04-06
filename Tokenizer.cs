using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using TextTokenizer.Entities;
using TextTokenizer.Enums;
using TextTokenizer.Interfaces;

namespace TextTokenizer
{
    public class Tokenizer : ITokenizer
    {
        private SupportedLanguages _currentLanguage = SupportedLanguages.Auto;
        private List<ITokenEntity> _resultTokens = new List<ITokenEntity>();
        private Dictionary<string, int> _rawTokens = new Dictionary<string, int>();
        private int _tokenMaxLength = 25;
        private int _tokenMinLength = 2;
        private readonly List<char> _charSeparators = new List<char>{ ' ', '+', '-', '/', '*', '~', '@', '#', '%', '^',
                                        '=', '<', '>', '!',
                                        ',', '.', ':', ';', '_',
                                        '$', '€', '£', '&', '?', '|', '\\', '\'', '§', '°', 
                                        '(', ')', '{', '}', '[', ']'};
        private Dictionary<SupportedLanguages, IEnumerable<string>> _whiteList = new Dictionary<SupportedLanguages, IEnumerable<string>>();
        private Dictionary<SupportedLanguages, IEnumerable<string>> _blackList = new Dictionary<SupportedLanguages, IEnumerable<string>>();

        private string _textToProcess;

        public bool IgnoreDigits { get; set; }

        public void SetTokenMinLength(int length)
        {
            _tokenMinLength = length;
        }

        public void SetTokenMaxLength(int length)
        {
            _tokenMaxLength = length;
        }

        public void AddSymbolSepatator(char separator)
        {
            if (_charSeparators != null && !_charSeparators.Contains(separator))
            {
                _charSeparators.Add(separator);
            }
        }

        public void AddWordToBlackList(SupportedLanguages language, string word)
        {
            AddWordToDict(ref _blackList, language, word);
        }

        public void AddWordToBlackList(SupportedLanguages language, IEnumerable<string> words)
        {
            AddListToDict(ref _blackList, language, words);
        }

        public void AddWordToWhiteList(SupportedLanguages language, string word)
        {
            AddWordToDict(ref _whiteList, language, word);
        }

        public void AddWordToWhiteList(SupportedLanguages language, IEnumerable<string> words)
        {
            AddListToDict(ref _whiteList, language, words);
        }

        public void SetLanguage(SupportedLanguages language)
        {
            _currentLanguage = language;
        }

        public void LoadText(string text)
        {
            _textToProcess = text.Replace(Environment.NewLine, " ");
        }

        public void LoadHtml(string htmlData)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlData);
            foreach (var script in doc.DocumentNode.Descendants("script").ToArray())
                script.Remove();
            foreach (var style in doc.DocumentNode.Descendants("style").ToArray())
                style.Remove();
            _textToProcess = doc.DocumentNode.InnerText;
        }

        public void Tokenize()
        {
            TryDetectLanguage();
            AddWordToBlackList(_currentLanguage, DefaultStopWords.GetStopWords(_currentLanguage));
            
            var tokens = _textToProcess.Split(_charSeparators.ToArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
            foreach (string token in tokens)
            {
                if (token.Length < _tokenMinLength || token.Length > _tokenMaxLength) continue;
                var normalizedToken = token.ToLower();
                if (!ValidateToken(normalizedToken)) continue;

                if (_rawTokens.ContainsKey(normalizedToken))
                {
                    _rawTokens[normalizedToken]++;
                }
                else
                {
                    _rawTokens.Add(normalizedToken, 1);
                }
            }
            foreach (KeyValuePair<string, int> pair in _rawTokens)
            {
                _resultTokens.Add(new TokenEntity(pair.Key, pair.Value));
            }
        }

        public IEnumerable<ITokenEntity> GetTokens()
        {
            return _resultTokens.OrderByDescending(x => x.Count);
        }

        public IEnumerable<ITokenEntity> GetTopTokens(int topValue)
        {
            return _resultTokens.OrderByDescending(x => x.Count);
        }

        public void PrintTokens()
        {
            foreach (ITokenEntity resultToken in _resultTokens.OrderBy(x => x.Count))
            {
                Console.Write(resultToken + " ");
            }
        }

        public void PrintTopTokens(int count)
        {
            foreach (ITokenEntity resultToken in _resultTokens.OrderByDescending(x => x.Count).Take(count))
            {
                Console.Write(resultToken + " ");
            }
        }

        private void AddListToDict(ref Dictionary<SupportedLanguages, IEnumerable<string>> dict, SupportedLanguages language,
            IEnumerable<string> words)
        {
            if (!dict.ContainsKey(language))
            {
                _blackList.Add(language, new List<string>(words));
            }
            else
            {
                List<string> list = _blackList[language].ToList();
                foreach (var word in words.Where(word => !list.Contains(word)))
                {
                    list.Add(word);
                }
            }
        }

        private void AddWordToDict(ref Dictionary<SupportedLanguages, IEnumerable<string>> dict, SupportedLanguages language,
            string word)
        {
            if (!dict.ContainsKey(language))
            {
                _blackList.Add(language, new List<string> { word });
            }
            else
            {
                var list = dict[language].ToList();
                if (list.Contains(word)) return;
                list.Add(word);
            }
        }

        private bool ValidateToken(string token)
        {
            if (_currentLanguage != SupportedLanguages.Auto)
            {
                if (_blackList.ContainsKey(_currentLanguage))
                {
                    if (_blackList[_currentLanguage].Contains(token))
                        return false;
                }
            }
            else
            {
                return _blackList.All(pair => !pair.Value.Contains(token));
            }
            return true;
        }

        private void TryDetectLanguage()
        {
            Dictionary<SupportedLanguages, int> languageRating = new Dictionary<SupportedLanguages, int>();
            foreach (SupportedLanguages language in Enum.GetValues(typeof(SupportedLanguages)))
            {
                if (language == SupportedLanguages.Auto) continue;

                var count = CalculateLanguegeSpecificOccurances(language);
                languageRating.Add(language, count);
            }
            _currentLanguage = languageRating.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
        }

        private int CalculateLanguegeSpecificOccurances(SupportedLanguages language)
        {
            /*
             * Method calculates occurances based on letter frequency table (http://en.wikipedia.org/wiki/Letter_frequency)
             * I got letters with frequency around 2%-5% on different languages
             * If you want to add another language, just find leter with frequency around 2 in http://www.bckelk.ukfsn.org/words/etaoin.html
             */
            switch (language)
            {
                case SupportedLanguages.En:
                    return GetCharCount(_textToProcess, 'y');
                case SupportedLanguages.Ru:
                    return GetCharCount(_textToProcess, 'ы');
                case SupportedLanguages.Ua:
                    return GetCharCount(_textToProcess, 'і');
                default:
                    return 0;
            }
        }

        private int GetCharCount(string _string, char _char)
        {
            return _string.Count(t => t == _char);
        }
    }
}
