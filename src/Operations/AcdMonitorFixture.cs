namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Synthetic data, invoked only by the Development-only UI fixture.</summary>
internal static class AcdMonitorFixture
{
    public static AcdBoardSample[] Create()
    {
        var now = DateTimeOffset.UtcNow;
        var context = Guid.Parse("11111111-1111-4111-8111-111111111111");
        var node = "22222222222242228222222222222222";
        var queue = new AcdBoardQueue("33333333333343338333333333333333", "Support · synthetic", 3, 10, now.AddMinutes(-2), 1, 1, 1);
        AcdContact[] contacts = [new("0000006001", "192.0.2.7", "10.0.0.7", "Synthetic Phone 1.2", now.AddMinutes(20))];
        AcdAgentPresence[] agents = [new(context.ToString("N"), "0000006001", now, true, true, false, contacts,
            [new(now.AddMinutes(-8), "observed", true, true, contacts), new(now.AddMinutes(-1), "renewed", true, true, contacts)], now.AddMinutes(-8)),
            new(context.ToString("N"), "0000006007", now, false, false, false),
            new(context.ToString("N"), "0000006008", now.AddMinutes(-5), true, true, false)];
        return [new("eveo-voip", context, new(context.ToString("N"), node, now, true, false, [queue],
            [new("44444444444444448444444444444444", queue.QueueId, "greeting", now.AddSeconds(-10), "synthetic.1", "PJSIP/6007-00000001", "6007")], agents), true),
            new("apoint-voip", context, null, false, Guid.Parse("55555555-5555-4555-8555-555555555555"))];
    }
}
