namespace Wendlewind.Sim;

public interface IHasContext
{
    GameContext Context { get; set; }
}

public interface IHasRng
{
    IRng Rng { get; set; }
}
