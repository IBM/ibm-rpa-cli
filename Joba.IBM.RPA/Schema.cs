namespace Joba.IBM.RPA
{
    internal record struct Region(string Name, string Description, string ApiUrl);
    internal record struct Session(string Token, int TenantCode, Guid TenantId, string TenantName, string PersonName);

    internal record struct Profile(string Token, int TenantCode, Guid TenantId, string TenantName, string PersonName, string UserName, string Password)
    {
        public static Profile Create(Account account, Session session)
        {
            return new Profile(session.Token, session.TenantCode, session.TenantId, session.TenantName, session.PersonName, account.UserName, account.Password);
        }
    }

    internal record class Tenant(Guid Id, int Code, string Name);
}
