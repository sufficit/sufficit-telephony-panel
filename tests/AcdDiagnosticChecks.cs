using Sufficit.Telephony.Panel.Operations;
static class AcdDiagnosticChecks
{
    public static void Run(Action<bool,string> check)
    {
        var context=Guid.NewGuid();var node=Guid.NewGuid();var visit=Guid.NewGuid();
        var eventPage=new AcdDiagnosticEvents(context,node,visit,[new(1,DateTimeOffset.UtcNow,"ringing","6018",null)],null,new(1,1,0,0,true));
        check(AcdDiagnosticsReader.Valid(eventPage,context,node),"Diagnostic event page accepted for exact tenant/node");
        check(!AcdDiagnosticsReader.Valid(eventPage with {ContextId=Guid.NewGuid()},context,node),"Diagnostic history never crosses tenant");
        check(!AcdDiagnosticsReader.Valid(eventPage with {NodeId=Guid.NewGuid()},context,node),"Diagnostic history never crosses node");
        check(!AcdDiagnosticsReader.Valid(eventPage with {Items=Enumerable.Repeat(eventPage.Items[0],101).ToArray()},context,node),"Diagnostic page has at most 100 events");
        var queues=Enumerable.Range(0,1000).Select(_=>new AcdBoardQueue(Guid.NewGuid().ToString("N"),"Synthetic",0,100,null)).ToArray();
        var visits=Enumerable.Range(0,5000).Select(i=>new AcdBoardVisit(Guid.NewGuid().ToString("N"),queues[i%queues.Length].QueueId,"waiting",DateTimeOffset.UtcNow)).ToArray();
        var snapshot=new AcdBoardSnapshot(context.ToString("N"),node.ToString("N"),DateTimeOffset.UtcNow,true,false,queues,visits);
        check(AcdBoardReader.ValidSnapshot(snapshot,context,node),"5,000 visits validated using indexed queue membership");
        var overview=AcdMonitorOverview.Build([new("synthetic",context,snapshot,true,node)],DateTimeOffset.UtcNow);
        check(overview.Visits.Length==5000 && overview.Queues.Length==1000,"Large monitoring projection preserves visits without per-visit queue scans");
    }
}
