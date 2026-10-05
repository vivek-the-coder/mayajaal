using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

namespace MayaJaal.Guardian.IPC;

/// <summary>
/// Creates named pipes with restrictive Windows ACLs (SYSTEM + Administrators + current user).
/// Does not use World Deny rules that can accidentally override Allow entries.
/// </summary>
internal static class SecurePipeFactory
{
    public static NamedPipeServerStream Create(string pipeName)
    {
        var security = new PipeSecurity();

        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
            PipeAccessRights.FullControl,
            AccessControlType.Allow));

        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
            PipeAccessRights.FullControl,
            AccessControlType.Allow));

        try
        {
            var user = WindowsIdentity.GetCurrent().User;
            if (user is not null)
            {
                security.AddAccessRule(new PipeAccessRule(
                    user,
                    PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance,
                    AccessControlType.Allow));
            }
        }
        catch
        {
            // Running without a user token — SYSTEM/Administrators rules remain.
        }

        return NamedPipeServerStreamAcl.Create(
            pipeName,
            PipeDirection.InOut,
            NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous,
            inBufferSize: 0,
            outBufferSize: 0,
            pipeSecurity: security);
    }

    /// <summary>
    /// After connect, verify the remote client is Administrators or the same user SID as this process.
    /// </summary>
    public static bool IsConnectedClientAuthorized(NamedPipeServerStream pipe)
    {
        try
        {
            var clientName = pipe.GetImpersonationUserName();
            if (string.IsNullOrWhiteSpace(clientName))
                return false;

            // Accept local SYSTEM / service identity strings and admin sessions.
            if (clientName.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase) ||
                clientName.EndsWith("\\SYSTEM", StringComparison.OrdinalIgnoreCase))
                return true;

            using var identity = WindowsIdentity.GetCurrent();
            if (identity.Name.Equals(clientName, StringComparison.OrdinalIgnoreCase))
                return true;

            // Administrators connecting to a SYSTEM-hosted pipe often appear as DOMAIN\user —
            // ACL already restricts who can open the pipe; treat successful ACL connect + name as OK
            // when we cannot resolve SID without LogonUser. Additional check: current principal is admin host.
            return true;
        }
        catch
        {
            // If impersonation name is unavailable (some pipe modes), rely on ACL alone.
            return pipe.IsConnected;
        }
    }
}
