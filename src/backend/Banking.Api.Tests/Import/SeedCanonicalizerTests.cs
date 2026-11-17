using Banking.Api.Import;
using CsvHelper;
using System.Globalization;

namespace Banking.Api.Tests.Import;

public sealed class SeedCanonicalizerTests
{
    [Fact]
    public void NormalizesEquivalentSourcesAndEveryOriginalFieldAffectsFingerprint()
    {
        Assert.Equal(1, SeedCanonicalizer.Version);
        var fields = new Dictionary<string, string>
        {
            ["id"] = "00209a31-c483-41bb-a9fd-35530f8f680b",
            ["customer_id"] = "fc99d3e9-f19d-439e-87e4-c7a54b585c7d",
            ["balance"] = "1.20",
            ["created_at"] = "2024-01-01T00:00:00.123456Z",
            ["date_of_birth"] = "2001-02-03",
            ["name"] = "กานต์",
            ["email"] = "",
            ["transfer_id"] = "",
            ["sequence"] = "1"
        };
        var headers = fields.Keys.ToArray();
        var expected = SeedCanonicalizer.Fingerprint(headers, fields);
        var equivalent = new Dictionary<string, string>(fields)
        {
            ["id"] = fields["id"].ToUpperInvariant(),
            ["balance"] = "01.2",
            ["created_at"] = "2024-01-01T07:00:00.123456+07:00",
            ["sequence"] = "01"
        };
        Assert.Equal(expected, SeedCanonicalizer.Fingerprint(headers, equivalent));
        var alternatives = new Dictionary<string, string>
        {
            ["id"] = Guid.NewGuid().ToString(),
            ["customer_id"] = Guid.NewGuid().ToString(),
            ["balance"] = "1.21",
            ["created_at"] = "2024-01-01T00:00:01.123456Z",
            ["date_of_birth"] = "2001-02-04",
            ["name"] = "กานต์ ",
            ["email"] = " ",
            ["transfer_id"] = Guid.NewGuid().ToString(),
            ["sequence"] = "2"
        };
        foreach (var header in headers)
        {
            var changed = new Dictionary<string, string>(fields) { [header] = alternatives[header] };
            Assert.NotEqual(expected, SeedCanonicalizer.Fingerprint(headers, changed));
        }
    }

    [Theory]
    [InlineData("customers")]
    [InlineData("accounts")]
    [InlineData("transfers")]
    [InlineData("transaction_entries")]
    public void EveryCsvFieldContributesToItsOriginalSourceFingerprint(string type)
    {
        using var reader = File.OpenText(Path.Combine(BaselineImportTests.SeedPath(), $"{type}.csv"));
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Read();
        csv.ReadHeader();
        var headers = csv.HeaderRecord!;
        csv.Read();
        var fields = headers.ToDictionary(name => name, name => csv.GetField(name) ?? "");
        var original = SeedCanonicalizer.Fingerprint(headers, fields);
        foreach (var header in headers)
        {
            var replacement = header switch
            {
                "id" or "customer_id" or "account_id" or "source_account_id" or "destination_account_id"
                    or "transfer_id" or "counterparty_account_id" => Guid.NewGuid().ToString(),
                "amount" or "balance" or "resulting_balance" => fields[header] == "1.00" ? "2.00" : "1.00",
                "sequence" => fields[header] == "1" ? "2" : "1",
                "created_at" or "completed_at" => "2000-01-01T00:00:00Z",
                "date_of_birth" => fields[header] == "2000-01-01" ? "2000-01-02" : "2000-01-01",
                _ => fields[header] + "x"
            };
            var changed = new Dictionary<string, string>(fields) { [header] = replacement };
            Assert.NotEqual(original, SeedCanonicalizer.Fingerprint(headers, changed));
        }
    }

    [Fact]
    public void EquivalentCsvQuotingDoesNotChangeFingerprint()
    {
        var sources = new[] { "customers", "accounts", "transfers", "transaction_entries" }
            .ToDictionary(type => type, type => File.ReadAllText(Path.Combine(BaselineImportTests.SeedPath(), $"{type}.csv")));
        var first = SeedCsvReader.ReadSources(type => new StringReader(sources[type]));
        var row = sources["accounts"].Split('\n', StringSplitOptions.RemoveEmptyEntries)[1].TrimEnd('\r');
        sources["accounts"] = sources["accounts"].Replace(row, row.Replace(",THB,", ",\"THB\",", StringComparison.Ordinal), StringComparison.Ordinal);
        var quoted = SeedCsvReader.ReadSources(type => new StringReader(sources[type]));
        Assert.Equal(first.Rows["accounts"][0].Fingerprint, quoted.Rows["accounts"][0].Fingerprint);
    }
}
