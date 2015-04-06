using System.Collections.Generic;
using TextTokenizer.Enums;

namespace TextTokenizer.Interfaces
{
    interface ITokenizer
    {
        bool IgnoreDigits { get; set; }
        void SetTokenMinLength(int length);
        void SetTokenMaxLength(int length);
        void AddSymbolSepatator(char separator);
        void AddWordToBlackList(SupportedLanguages language, string word);
        void AddWordToBlackList(SupportedLanguages language, IEnumerable<string> words);
        void AddWordToWhiteList(SupportedLanguages language, string word);
        void AddWordToWhiteList(SupportedLanguages language, IEnumerable<string> words);
        void SetLanguage(SupportedLanguages language);
        void LoadText(string text);
        void LoadHtml(string htmlData);
        void Tokenize();

        IEnumerable<ITokenEntity> GetTokens();
        void PrintTokens();
    }
}
