namespace Grafted.Sim.Entities.Items;

public class FoodProperties {
    public FoodType FoodType;
    public bool CanEat => TicksToIngest > 0;
    public int TicksToIngest;
}