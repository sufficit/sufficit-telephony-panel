using Sufficit.Telephony.Monitor;

namespace Sufficit.Telephony.Panel.Operations;

public sealed record BoardSupervisionAction(OperatorTile Tile, TelephonyMonitorActionMode Mode, bool Continuous);
