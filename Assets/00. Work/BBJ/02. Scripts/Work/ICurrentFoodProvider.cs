using BBJ.Data;

namespace BBJ.Work
{
    public interface ICurrentFoodProvider
    {
        FoodDataSO CurrentFood { get; }
    }
}
