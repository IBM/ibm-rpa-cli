namespace Joba.IBM.RPA.Server
{
    public record class Tenant(Guid Id, int Code, string Name);
    public record class ComputerGroup(Guid Id, string Name);
    public record class Computer(Guid Id, string Name);
    public record class CreateBotRequest(Guid ProjectId, Guid ScriptId, Guid ScriptVersionId, [property: JsonPropertyName("GroupId")] Guid ComputerGroupId, string Name, [property: JsonPropertyName("TechnicalName")] UniqueId UniqueId, string Description)
    {
        public static CreateBotRequest Copy(CreateBotRequest bot, UniqueId uniqueId) =>
            new(bot.ProjectId, bot.ScriptId, bot.ScriptVersionId, bot.ComputerGroupId, bot.Name, uniqueId, bot.Description);
    }

    public record class Project(Guid Id, string Name, string Description, [property: JsonPropertyName("TechnicalName")] string UniqueId);
    public record class Parameter([property: JsonPropertyName("Id")] string Name, string Value);
    public record class PublishScript(Guid? Id, Guid? VersionId, string Name, string? Description, string Content, string ProductVersion,
        bool SetAsProduction, int GreenExecutionTimeSeconds, int YellowExecutionTimeSeconds, int RedExecutionTimeSeconds);

    public record class Script(Guid Id, string Name);
    public record class ScriptVersion(Guid Id, Guid ScriptId, string Name, WalVersion Version, Version ProductVersion, string Content);

    public record class CreateChatMappingRequest([property: JsonPropertyName("BotId")] Guid ChatId, Guid ScriptId, Guid ScriptVersionId, string Name, string? Greeting, [property: JsonPropertyName("StyleOptions")] string? Style, Guid[] ComputersId, bool UnlockMachine);
    public record class Chat(Guid Id, [property: JsonPropertyName("BotHandle")] string Handle);
    public record class ChatMapping(Guid Id, [property: JsonPropertyName("BotId")] Guid ChatId, string Name, [property: JsonPropertyName("BotHandle")] string Handle);

}
