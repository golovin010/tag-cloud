using System;
using TextTokenizer.Interfaces;

namespace TextTokenizer.Entities
{
    class TokenEntity : ITokenEntity
    {
        public string Value { get; set; }

        public int Count { get; set; }
        public override string ToString()
        {
            return String.Format("{0}({1})", Value, Count);
        }

        public TokenEntity(string value, int count)
        {
            Value = value;
            Count = count;
        }
    }
}
