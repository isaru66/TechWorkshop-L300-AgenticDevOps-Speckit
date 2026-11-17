namespace Banking.Api.Data.Entities;

public sealed class SeedRecord
{
    public required string EntityType { get; set; }
    public Guid RecordId { get; set; }
    public int CanonicalVersion { get; set; }
    public required string SourceFingerprint { get; set; }
    public DateTimeOffset ImportedAt { get; set; }
}
