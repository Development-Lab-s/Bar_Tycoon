using _00._Work._Resources._02._Scripts.Modules;
using System;

public interface ILPBOX
{
    event Action<int> OnLPClicked;

    void Select();
    void SetUp(int id);
    void Stop();
}