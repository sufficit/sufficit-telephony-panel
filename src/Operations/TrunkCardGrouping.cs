using Sufficit.Telephony.Panel;
namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Presentation grouping after authorization. Each original row retains its node and company.</summary>
public static class TrunkCardGrouping
{
    public static OperatorTile[] Merge(IEnumerable<OperatorTile> source)
    {
        var tiles=source.ToArray();
        var trunks=tiles.Where(t=>t.Kind==EventsPanelCardKind.TRUNK && InboundCatalogueEntry.State(t.Keys) is null && !string.IsNullOrWhiteSpace(t.Title)).ToArray();
        var grouped=trunks.GroupBy(t=>string.Join(' ',t.Title.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant(),StringComparer.Ordinal)
            .Select(g=> {
                var preferred=g.OrderByDescending(t=>t.HasObservedActivity).ThenByDescending(t=>t.Updated).ThenBy(t=>t.Title,StringComparer.Ordinal).First();
                var nodes=g.Select(t=>t.Node).Distinct(StringComparer.Ordinal).ToArray();
                return new OperatorTile("trunk-group|"+g.Key,preferred.Title,nodes.Length==1?nodes[0]:"",EventsPanelCardKind.TRUNK,
                    g.SelectMany(t=>t.Rows).GroupBy(r=>(r.Node,r.Id)).Select(r=>r.OrderByDescending(x=>x.Updated).First()).ToArray()) {
                    Keys=g.SelectMany(t=>t.Keys).Distinct(StringComparer.Ordinal).ToArray(),
                    Flows=g.SelectMany(t=>t.Flows).DistinctBy(f=>f.Id).ToArray(),LastSeen=g.Max(t=>t.Updated)
                };
            });
        var selected=trunks.ToHashSet();
        return tiles.Where(t=>!selected.Contains(t)).Concat(grouped).ToArray();
    }
}
