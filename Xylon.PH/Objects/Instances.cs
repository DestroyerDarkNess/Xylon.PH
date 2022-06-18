using ProcessHacker.Common;
using ProcessHacker.Native;
using ProcessHacker.Native.Api;
using ProcessHacker.Native.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Xylon.PH.Objects
{
    public static class Instances
    {
        public static IdGenerator ResultsIds = new IdGenerator() { Sort = true };

        public static Dictionary<string, ProcessHacker.Structs.StructDef> Structs = new Dictionary<string, ProcessHacker.Structs.StructDef>();


        public static TokenElevationType GetElevationPrivilege()
        {

          TokenElevationType ElevationType = TokenElevationType.Limited;

            try
            {
                using (var thandle = ProcessHandle.Current.GetToken())
                {
                    thandle.TrySetPrivilege("SeDebugPrivilege", SePrivilegeAttributes.Enabled);
                    thandle.TrySetPrivilege("SeIncreaseBasePriorityPrivilege", SePrivilegeAttributes.Enabled);
                    thandle.TrySetPrivilege("SeLoadDriverPrivilege", SePrivilegeAttributes.Enabled);
                    thandle.TrySetPrivilege("SeRestorePrivilege", SePrivilegeAttributes.Enabled);
                    thandle.TrySetPrivilege("SeShutdownPrivilege", SePrivilegeAttributes.Enabled);
                    thandle.TrySetPrivilege("SeTakeOwnershipPrivilege", SePrivilegeAttributes.Enabled);

                    if (OSVersion.HasUac)
                    {
                        try { ElevationType = thandle.GetElevationType(); }
                        catch { ElevationType = TokenElevationType.Full; }

                        if (ElevationType == TokenElevationType.Default &&
                            !(new WindowsPrincipal(WindowsIdentity.GetCurrent())).
                                IsInRole(WindowsBuiltInRole.Administrator))
                            ElevationType = TokenElevationType.Limited;
                        else if (ElevationType == TokenElevationType.Default)
                            ElevationType = TokenElevationType.Full;
                    }
                    else
                    {
                        ElevationType = TokenElevationType.Full;
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Log(ex);
            }

            return ElevationType;
        }


    }
}
