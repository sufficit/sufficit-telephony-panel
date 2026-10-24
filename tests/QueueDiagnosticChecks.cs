using Sufficit.Telephony.Panel.Operations;
static class QueueDiagnosticChecks
{
    public static void Run(Action<bool,string> check)
    {
        var context=Guid.NewGuid();var node=Guid.NewGuid();var visit=Guid.NewGuid();
        var eventPage=new QueueDiagnosticEvents(context,node,visit,[new(1,DateTimeOffset.UtcNow,"ringing","6018",null)],null,new(1,1,0,0,true));
        check(QueueDiagnosticsReader.Valid(eventPage,context,node),"Diagnostic event page accepted for exact tenant/node");
        check(!QueueDiagnosticsReader.Valid(eventPage with {ContextId=Guid.NewGuid()},context,node),"Diagnostic history never crosses tenant");
        check(!QueueDiagnosticsReader.Valid(eventPage with {NodeId=Guid.NewGuid()},context,node),"Diagnostic history never crosses node");
        check(!QueueDiagnosticsReader.Valid(eventPage with {Items=Enumerable.Repeat(eventPage.Items[0],101).ToArray()},context,node),"Diagnostic page has at most 100 events");
        var shift=new QueueShiftReport(context,node,20,[new(new DateOnly(2026,10,23),Guid.NewGuid(),10,8,1,1,12.5m,7)],[new(Guid.NewGuid(),"6004",5)]);
        check(QueueDiagnosticsReader.Valid(shift,context,node),"Shift report accepted for exact tenant/node");
        check(!QueueDiagnosticsReader.Valid(shift with {ContextId=Guid.NewGuid()},context,node),"Shift report never crosses tenant");
        check(!QueueDiagnosticsReader.Valid(shift with {Days=[shift.Days[0] with {Answered=11}]},context,node),"Shift report rejects answered above entered");
        var shiftJson=System.Text.Json.JsonSerializer.Serialize(new{contextId=context,nodeId=node,window=new{start="08:00:00",weekdays=new[]{1,2}},slaSeconds=20,
            days=new[]{new{date="2026-10-23",queueId=Guid.NewGuid(),entered=3,answered=2,abandoned=1,timedOut=0,averageWaitSeconds=(decimal?)9.5m,answeredWithinSla=2}},extensions=Array.Empty<object>()});
        var parsed=System.Text.Json.JsonSerializer.Deserialize<QueueShiftReport>(shiftJson,new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        check(parsed is {Days.Length:1} && parsed.Days[0].Date==new DateOnly(2026,10,23) && QueueDiagnosticsReader.Valid(parsed,context,node),"Controller shift JSON parses (extra window field ignored)");
        var morning=new QueueConfigShift("Manhã","08:00","14:00",[1,2,3,4,5]);var night=new QueueConfigShift("Noite","22:00","06:00",[5]);
        var friday=new DateTimeOffset(2026,10,23,11,0,0,TimeSpan.Zero); // 08:00 local (UTC-3)
        check(morning.Contains(friday,out var fd)&&fd==new DateOnly(2026,10,23)&&!morning.Contains(friday.AddHours(6),out _),"Shift window uses local time, end exclusive");
        check(night.Contains(new DateTimeOffset(2026,10,24,5,0,0,TimeSpan.Zero),out var nd)&&nd==new DateOnly(2026,10,23),"Night shift after midnight belongs to the day it started");
        var shiftQueue=Guid.NewGuid();
        var shiftReport=new QueueShiftReport(context,node,20,[new(new DateOnly(2026,10,22),shiftQueue,10,9,1,0,8m,8),new(new DateOnly(2026,10,23),shiftQueue,20,15,5,0,9m,12),new(new DateOnly(2026,10,23),Guid.NewGuid(),99,99,0,0,1m,99)],
            [new(shiftQueue,"6004",7),new(shiftQueue,"6008",9),new(Guid.NewGuid(),"7000",50)]);
        var view=QueueShiftView.Build(morning,shiftReport,shiftQueue,friday.AddHours(1));
        check(view.Current&&view.Entered==20&&view.Days.Length==2&&view.TopExtensions[0].Extension=="6008"&&view.TopExtensions.Length==2,"Shift view keeps only the selected queue, today and top extensions");
        check(QueueShiftView.Build(morning,null,shiftQueue,friday).Entered==0,"Shift without report still renders with zero");
        var saturday=QueueShiftView.Build(morning,shiftReport,shiftQueue,new DateTimeOffset(2026,10,24,15,0,0,TimeSpan.Zero));
        check(!saturday.IsToday&&saturday.ShownDate==new DateOnly(2026,10,23)&&saturday.Entered==20,"Shift that does not run today shows its last run with the date");
        check(morning.ReportQuery()=="report/shift?start=08:00&end=14:00&days=1,2,3,4,5&offset=-180","Shift report query matches controller parameters");
        var queues=Enumerable.Range(0,1000).Select(_=>new QueueBoardQueue(Guid.NewGuid().ToString("N"),"Synthetic",0,100,null)).ToArray();
        var visits=Enumerable.Range(0,5000).Select(i=>new QueueBoardVisit(Guid.NewGuid().ToString("N"),queues[i%queues.Length].QueueId,"waiting",DateTimeOffset.UtcNow)).ToArray();
        var snapshot=new QueueBoardSnapshot(context.ToString("N"),node.ToString("N"),DateTimeOffset.UtcNow,true,false,queues,visits);
        check(QueueBoardReader.ValidSnapshot(snapshot,context,node),"5,000 visits validated using indexed queue membership");
        var overview=QueueMonitorOverview.Build([new("synthetic",context,snapshot,true,node)],DateTimeOffset.UtcNow);
        check(overview.Visits.Length==5000 && overview.Queues.Length==1000,"Large monitoring projection preserves visits without per-visit queue scans");
        // Owner rule (ari 0.20.5): extensions no presence source watches ring without a presence check.
        QueueAgentPresence Agent(string extension,bool monitored)=>new(context.ToString("N"),extension,DateTimeOffset.UtcNow,null,null,null,Monitored:monitored);
        QueueBoardSnapshot Agents(Guid id,params QueueAgentPresence[] agents)=>new(context.ToString("N"),id.ToString("N"),DateTimeOffset.UtcNow,true,false,[],[],agents);
        var other=Guid.NewGuid();
        var watch=QueueMonitorOverview.Build([new("eveo",context,Agents(node,Agent("1001",true),Agent("1002",false)),true,node),
            new("google",context,Agents(other,Agent("1001",true),Agent("1002",true)),true,other)],DateTimeOffset.UtcNow).Agents.ToDictionary(a=>a.Extension);
        check(watch["1001"].UnwatchedNodes.Length==0 && watch["1002"].UnwatchedNodes.SequenceEqual(["eveo"]) && watch["1002"].WatchedNodes.SequenceEqual(["google"]),
            "Presence monitoring is reported per node for queue screens");
        check(new QueueAgentPresence(context.ToString("N"),"1003",DateTimeOffset.UtcNow,null,null,null).Monitored,"Older nodes without the field read as watched");
    }
}
