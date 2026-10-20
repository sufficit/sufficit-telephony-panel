namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Synthetic data, invoked only by the Development-only UI fixture.</summary>
internal static class QueueMonitorFixture
{
    public static QueueDiagnosticCalls Calls(Guid context,Guid node)
    {
        var now=DateTimeOffset.UtcNow;
        return new(context,node,[new(Guid.ParseExact("44444444444444448444444444444444","N"),Guid.ParseExact("33333333333343338333333333333333","N"),"synthetic.1","completed",now.AddSeconds(-90),now.AddSeconds(-50),now.AddSeconds(-24))],null);
    }
    public static QueueDiagnosticEvents Events(Guid context,Guid node,Guid visit)
    {
        var now=DateTimeOffset.UtcNow;
        return new(context,node,visit,[new(1,now.AddSeconds(-90),"entered",null,null),new(2,now.AddSeconds(-75),"offered","6018",null),new(3,now.AddSeconds(-60),"offer_finished","6018","NOANSWER"),new(4,now.AddSeconds(-50),"connected","6019",null),new(5,now.AddSeconds(-24),"finished",null,"completed")],null,new(5,5,0,0,true));
    }
    public static QueueDiagnosticReport Report(Guid context,Guid node)=>new(context,node,[new(Guid.ParseExact("33333333333343338333333333333333","N"),20,16,2,2,12.5m,14,20)]);
    public static QueueBoardSample[] Create()
    {
        var now = DateTimeOffset.UtcNow;
        var context = Guid.Parse("11111111-1111-4111-8111-111111111111");
        var node = "22222222222242228222222222222222";
        var queue = new QueueBoardQueue("33333333333343338333333333333333", "Support · synthetic", 3, 10, now.AddMinutes(-2), 1, 1, 1);
        QueueContact[] contacts = [new("0000006001", "192.0.2.7", "10.0.0.7", "Synthetic Phone 1.2", now.AddMinutes(20))];
        QueueAgentPresence[] agents = [new(context.ToString("N"), "0000006001", now, true, true, false, contacts,
            [new(now.AddMinutes(-8), "observed", true, true, contacts), new(now.AddMinutes(-1), "renewed", true, true, contacts)], now.AddMinutes(-8)),
            new(context.ToString("N"), "0000006007", now, false, false, false),
            new(context.ToString("N"), "0000006008", now.AddMinutes(-5), true, true, false)];
        return [new("eveo-voip", context, new(context.ToString("N"), node, now, true, false, [queue],
            [new("44444444444444448444444444444444", queue.QueueId, "greeting", now.AddSeconds(-10), "synthetic.1", "PJSIP/6007-00000001", "6007")], agents), true),
            new("google-voip", context, new(context.ToString("N"), "66666666666646668666666666666666", now, true, false,
                [queue with { Waiting = 2, Greeting = 0, Offering = 0 }], [],
                [agents[0] with { Registered = false, Reachable = false, Contacts = [], RegistrationHistory =
                    [new(now.AddSeconds(-30), "unregistered", false, false, [])] }]), true),
            new("apoint-voip", context, null, false, Guid.Parse("55555555-5555-4555-8555-555555555555"))];
    }
}
