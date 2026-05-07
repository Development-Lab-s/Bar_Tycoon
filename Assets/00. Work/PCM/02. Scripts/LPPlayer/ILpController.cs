using _00._Work._Resources._02._Scripts.Modules;

public interface ILpController : IModule , IAfterInitModule
{
    void PlayLp(int id);
}