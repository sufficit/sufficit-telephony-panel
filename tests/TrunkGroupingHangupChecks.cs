using Sufficit.Telephony.Panel;
using Sufficit.Telephony.Panel.Operations;
static class TrunkGroupingHangupChecks
{
    public static void Run(Action<bool,string> check)
    {
        var now=DateTimeOffset.UtcNow;
        OperatorTile Tile(string node,string title) => new(node,title,node,EventsPanelCardKind.TRUNK,[new("same-id",node,"call","SIP/provider-000000ef","Up","6001","7001",null,now,now,new Dictionary<string,string>{["uniqueid"]="1234567890.1"})]){Keys=["SIP/provider"]};
        var a=Tile("google-voip","DATORA");var b=Tile("eveo-voip"," datora ");
        var grouped=TrunkCardGrouping.Merge([a,b]);
        check(grouped.Length==1 && grouped[0].Rows.Length==2,"Same provider across nodes produces one card with both original channels");
        check(grouped[0].DetailChannels.Length==2 && BoardTotals.ActiveCalls(grouped)==2,"Same channel identity on different nodes remains distinct for actions and totals");
        check(TrunkCardGrouping.Merge([a]).Single().Id==grouped.Single().Id,"Provider selection identity survives a node filter");
        check(TrunkCardGrouping.Merge([a,b,a with{Id="alias"}]).Single().Rows.Length==2,"Duplicate portal aliases never double provider channels");
        var targets=SharedSupervisionPrefix.TargetsFor(grouped[0],true,true);
        check(targets.Length==2 && targets.All(t=>t.Prefix=="SIP/provider-"),"Continuous trunk supervision offers exact original nodes independently");
        check(SharedSupervisionPrefix.TargetsFor(grouped[0],false,true).Length==0,"Provider grouping grants no shared supervision permission");
        check(TrunkCardGrouping.Merge([a,Tile("eveo","GTGI")]).Length==2,"Different providers stay separate");
        var history=b with{Rows=[],LastSeen=now.AddDays(-1)};
        check(TrunkCardGrouping.Merge([a,history]).Single().HasObservedActivity,"Historic provider replica cannot hide an active node");
        var row=a.Rows.Single();
        check(ChannelHangupGuard.BlockReason(row,true,true)==null,"Manager may terminate a verified trunk channel without a customer account or preferred extension");
        check(ChannelHangupGuard.BlockReason(row,false,true)!=null && ChannelHangupGuard.BlockReason(row,true,false)!=null,"Hangup requires current manager permission and a connected stream");
        check(ChannelHangupGuard.BlockReason(row with{Key="Local/123@context-00001;1"},true,true)==null,"Local channel termination does not inherit SIP-only supervision restriction");
        check(ChannelHangupGuard.BlockReason(row with{Details=new Dictionary<string,string>()},true,true)!=null,"Missing unique ID blocks termination");
        check(ChannelHangupGuard.BlockReason(row with{Key="SIP/*"},true,true)!=null,"Prefix/glob cannot terminate channels");
        var peer=a with{Kind=EventsPanelCardKind.PEER};check(TrunkCardGrouping.Merge([peer,a]).Contains(peer),"Extension grouping is unaffected");
    }
}
