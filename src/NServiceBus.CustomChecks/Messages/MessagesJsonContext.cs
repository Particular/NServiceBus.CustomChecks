namespace ServiceControl.Plugin.CustomChecks.Messages;

using System.Text.Json.Serialization;

[JsonSerializable(typeof(ReportCustomCheckResult))]
partial class MessagesJsonContext : JsonSerializerContext;