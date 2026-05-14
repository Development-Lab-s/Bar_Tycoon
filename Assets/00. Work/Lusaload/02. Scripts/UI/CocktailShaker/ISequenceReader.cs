using System;

namespace _00._Work.Lusaload._02._Scripts.UI.CocktailShaker
{
    public interface ISequenceReader
    {
        CocktailOrderSequence CurrentSequence { get; }
        event Action<CocktailOrderSequence> OnSequenceChanged;
    }
}