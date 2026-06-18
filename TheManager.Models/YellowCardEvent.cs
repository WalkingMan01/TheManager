namespace TheManager.Models;

/// <summary>A pre-rolled yellow-card event: which starting slot, and at what minute.</summary>
public class YellowCardEvent
{
    public int Minute { get; set; }
    public int Slot   { get; set; }   // 1-12, resolved at roll time
}
