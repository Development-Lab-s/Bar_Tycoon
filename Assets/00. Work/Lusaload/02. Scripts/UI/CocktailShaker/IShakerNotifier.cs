using System;

namespace _00._Work.Lusaload._02._Scripts.UI.CocktailShaker
{
    public interface IShakerNotifier
    {
        event Action OnShakerFull;
    }
}
