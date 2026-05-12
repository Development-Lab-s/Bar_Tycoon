using _00._Work._Resources._02._Scripts.Modules;
using Gamelib.SoundSystem;

public interface ILP
{
    void Active();
    void Stop();
    void ApplyPosition();
    void PlaySound(BgmSounds id);
    string NameChosing(int id);
    void StopExistingMotions();
}