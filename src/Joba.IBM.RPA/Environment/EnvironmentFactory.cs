
namespace Joba.IBM.RPA
{
    internal static class EnvironmentFactory
    {
        internal static Environment Create(UserSettingsFile userFile, UserSettings userSettings,
            string alias, Region region, CreatedSession session)
        {
            var remote = RemoteSettings.Create(region, session);
            userSettings.Sessions.Add(alias, Session.From(session));
            
            return new Environment(alias, remote, userFile, userSettings);
        }
    }
}
