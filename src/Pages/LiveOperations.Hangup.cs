using Sufficit.Telephony.Panel.Operations;
using Sufficit.Blazor.UI.Components;
namespace Sufficit.Telephony.Panel.Pages;

public partial class LiveOperations
{
    private ObservedResource? HangupPending;
    private string? HangupBlockReason(ObservedResource row) => ActionBusy ? "A request is already in progress."
        : ChannelHangupGuard.BlockReason(row,Permissions.Manager,Connected)
        ?? (!Rows.Any(r=>r.Id==row.Id && r.Node==row.Node && r.Key==row.Key
            && r.Details.GetValueOrDefault("uniqueid")==row.Details.GetValueOrDefault("uniqueid"))
            ? "The selected channel ended or changed. Refresh before trying again." : null);
    private void PrepareHangup(ObservedResource row)
    {
        if(HangupBlockReason(row) is not null) return;
        Pending=null; HangupPending=row; ActionMessage=null; FocusConfirmation=true;
    }
    private async Task ConfirmHangup()
    {
        if(HangupPending is not { } row || HangupBlockReason(row) is not null) return;
        ActionBusy=true; HangupPending=null; ActionMessage=null; ActionFailure=null;
        try {
            if(Demo) {
                Toast.Add(Text["Demonstration: termination confirmed. No channel was ended."],SUITone.Info,8000);
                return;
            }
            await Access.Hangup(row,lifetime.Token);
            Toast.Add(Text["Channel termination accepted. Wait for the channel to disappear from the event stream."],SUITone.Success,10000);
        }
        catch(Exception e) {
            ActionFailure=Diagnostics.Report(e,"channel-hangup");
            ActionMessage=Text["Channel termination was not confirmed. Refresh the channel status before trying again. No automatic retry."];
            Toast.Add(ActionMessage,SUITone.Warning,12000);
        }
        finally {ActionBusy=false;}
    }
}
