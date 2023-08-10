
namespace Joba.IBM.RPA
{
    internal static class EnvironmentFactory
    {
        internal static Environment Create(UserSettingsFile userFile, UserSettings userSettings,
            string alias, Region region, CreatedSession session, ServerConfig server, PropertyOptions properties)
        {
            var remote = RemoteSettings.Create(region, session, server, properties);
            userSettings.AddOrUpdateSession(alias, Session.From(session));

            return new Environment(alias, remote, userFile, userSettings);
        }
    }
}
