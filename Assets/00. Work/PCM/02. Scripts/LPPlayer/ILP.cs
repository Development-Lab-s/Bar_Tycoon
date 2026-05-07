using _00._Work._Resources._02._Scripts.Modules;
using Gamelib.SoundSystem;

public interface ILP
{
    BgmSounds sound { get; set; }
    void Active();
    void Stop();
}