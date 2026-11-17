using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;

namespace Banking.Api.Import;

public static class SeedCanonicalizer
{
    public const int Version = 1;

    public static string Fingerprint(IReadOnlyList<string> headers, IReadOnlyDictionary<string, string> fields)
    {
        var values = new string?[headers.Count];
        for (var index = 0; index < headers.Count; index++)
        {
            var name = headers[index];
            var value = fields[name];
            values[index] = name switch
            {
                "id" or "customer_id" or "account_id" or "source_account_id" or "destination_account_id"
                    => Guid.Parse(value).ToString("D"),
                "transfer_id" or "counterparty_account_id"
                    => value.Length == 0 ? null : Guid.Parse(value).ToString("D"),
                "amount" or "balance" or "resulting_balance"
                    => decimal.Parse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture)
                        .ToString("F2", CultureInfo.InvariantCulture),
                "sequence" => long.Parse(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
                "created_at" or "completed_at" => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture)
                    .ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
                "date_of_birth" => value.Length == 0 ? null
                    : DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                        .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                _ => value
            };
        }

        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(values))).ToLowerInvariant();
    }
}
