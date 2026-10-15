using System.Text.Json;
using Sufficit.Telephony.Panel.Operations;
internal static class AccountingChecks
{
    public static async Task Run(Action<bool,string> check)
    {
        var context=Guid.NewGuid();var id=$"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.123";
        var http=new FakeHttp{Body=JsonSerializer.Serialize(new QueueAccounting(context,id,"google-voip","confirmed",26,30,.14m,.14m))};
        var api=new OperationsApi(http);
        var actual=await api.Accounting("synthetic",context,"google-voip",id,default);
        check(actual.RecordedSeconds==26 && actual.TariffReferenceSeconds==30 && actual.Debited==.14m,"Recorded conversation input, tariff reference and actual debit stay separate");
        http.Body=JsonSerializer.Serialize(actual with{ContextId=Guid.NewGuid()});
        try{await api.Accounting("synthetic",context,"google-voip",id,default);throw new Exception("foreign accounting accepted");}
        catch(InvalidDataException){check(true,"Foreign accounting context rejected");}
        http.Body=JsonSerializer.Serialize(actual with{Node="other"});
        try{await api.Accounting("synthetic",context,"google-voip",id,default);throw new Exception("foreign node accepted");}
        catch(InvalidDataException){check(true,"Foreign accounting node rejected");}
        var requests=http.Requests;
        try{await api.Accounting("synthetic",context,"google-voip","uncorrelated-agent-leg",default);throw new Exception("invalid ID accepted");}
        catch(InvalidOperationException){check(http.Requests==requests,"Non-correlatable channel never triggers broad accounting query");}
    }
}
