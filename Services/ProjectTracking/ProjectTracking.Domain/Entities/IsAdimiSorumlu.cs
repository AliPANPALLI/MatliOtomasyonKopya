namespace ProjectTracking.Domain.Entities;

public sealed class IsAdimiSorumlu
{
    public int IsAdimiId { get; set; }
    public int SorumluId { get; set; }
    public DateTime AtanmaUtc { get; set; }
    public IsAdimi IsAdimi { get; set; } = null!;
    public Sorumlu Sorumlu { get; set; } = null!;
}
