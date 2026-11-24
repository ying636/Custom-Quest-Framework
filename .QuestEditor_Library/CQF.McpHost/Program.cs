using System.Text.Json;

if (args.Length != 2 || args[0] != "--pipe" || !args[1].StartsWith("CQF-", StringComparison.Ordinal) ||
    !Guid.TryParseExact(args[1].AsSpan(4), "N", out _))
{
    Console.Error.WriteLine("Usage: CQF.McpHost --pipe CQF-<id shown in RimWorld>");
    return 2;
}

CqfMcpServer server = new CqfMcpServer(args[1]);
while (await Console.In.ReadLineAsync() is { } line)
{
    if (line.Length > 524288)
    {
        Console.WriteLine(JsonSerializer.Serialize(CqfMcpServer.Error(null, -32600, "Request too large")));
        continue;
    }
    try
    {
        using JsonDocument request = JsonDocument.Parse(line);
        object? response = await server.Dispatch(request.RootElement);
        if (response != null) Console.WriteLine(JsonSerializer.Serialize(response));
    }
    catch (JsonException)
    {
        Console.WriteLine(JsonSerializer.Serialize(CqfMcpServer.Error(null, -32700, "Parse error")));
    }
}
return 0;
