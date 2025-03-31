namespace Grafted.Sim.Combat;

public class AfflictionRecord(BodyPart bodyPart, string label)
{
    public readonly BodyPart BodyPart = bodyPart;
    public readonly string Label = label;
}