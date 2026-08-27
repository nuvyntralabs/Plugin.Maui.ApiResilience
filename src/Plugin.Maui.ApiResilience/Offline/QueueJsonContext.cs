namespace Plugin.Maui.ApiResilience;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(QueueState))]
[JsonSerializable(typeof(QueuedRequest))]
[JsonSerializable(typeof(List<QueuedRequest>))]
internal partial class QueueJsonContext : JsonSerializerContext;
