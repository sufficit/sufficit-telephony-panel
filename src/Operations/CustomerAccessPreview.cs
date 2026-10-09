using System.Collections.Immutable;
using Microsoft.AspNetCore.WebUtilities;
using Sufficit.Identity.Authorization;

namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Synthetic UI fixture. Call only inside the existing Development preview.</summary>
public static class CustomerAccessPreview
{
    public static readonly Guid First = Guid.Parse("11111111-1111-7111-8111-111111111111");
    public static readonly Guid Second = Guid.Parse("22222222-2222-7222-8222-222222222222");
    public static CurrentAuthorization Read(string uri)
    {
        var query = QueryHelpers.ParseQuery(new Uri(uri).Query);
        var mode = query.TryGetValue("previewaccess", out var value) ? value.ToString() : "";
        return mode switch
        {
            "single" => new(false, ImmutableHashSet.Create(First), []),
            "multiple" => new(false, ImmutableHashSet.Create(First, Second), []),
            "global" => new(false, ImmutableHashSet.Create(Guid.Empty), []),
            _ => new(true, [], [])
        };
    }
}
