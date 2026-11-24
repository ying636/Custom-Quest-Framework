using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

internal sealed class CqfMcpServer
{
    public CqfMcpServer(string pipeName)
    {
        this.pipeName = pipeName;
    }

    public async Task<object?> Dispatch(JsonElement request)
    {
        if (request.ValueKind != JsonValueKind.Object)
            return Error(null, -32600, "Invalid request");
        JsonElement? id = request.TryGetProperty("id", out JsonElement requestId) ? requestId : null;
        if (!request.TryGetProperty("jsonrpc", out JsonElement version) || version.ValueKind != JsonValueKind.String || version.GetString() != "2.0" ||
            !request.TryGetProperty("method", out JsonElement method) || method.ValueKind != JsonValueKind.String ||
            id is { ValueKind: not (JsonValueKind.String or JsonValueKind.Number or JsonValueKind.Null) })
            return Error(null, -32600, "Invalid request");
        if (!id.HasValue) return null;
        request.TryGetProperty("params", out JsonElement parameters);
        try
        {
            object result;
            switch (method.GetString())
            {
                case "initialize":
                    string protocol = OptionalString(parameters, "protocolVersion");
                    result = new
                    {
                        protocolVersion = protocol is "2024-11-05" or "2025-03-26" or "2025-06-18" or "2025-11-25" ? protocol : "2025-06-18",
                        capabilities = new { tools = new { } },
                        serverInfo = new { name = "cqf", version = "1.0.0" }
                    };
                    break;
                case "ping":
                    result = new { };
                    break;
                case "tools/list":
                    result = new { tools = Tools };
                    break;
                case "tools/call":
                    string name = RequiredString(parameters, "name");
                    JsonElement arguments = parameters.TryGetProperty("arguments", out JsonElement provided) ? provided : default;
                    XElement command = BuildCommand(name, arguments);
                    try
                    {
                        XElement reply = await CallGame(command);
                        result = ToolResult(reply.ToString(SaveOptions.DisableFormatting),
                            reply.Name == "error" || (bool?)reply.Attribute("success") == false);
                    }
                    catch (Exception error) when (error is IOException or TimeoutException or OperationCanceledException or UnauthorizedAccessException or System.Xml.XmlException)
                    {
                        result = ToolResult("CQF MCP bridge unavailable or interrupted: " + error.Message +
                            " If a write was submitted, retry with the same request_id.", true);
                    }
                    break;
                default:
                    return Error(id, -32601, "Method not found");
            }
            return new { jsonrpc = "2.0", id, result };
        }
        catch (Exception error) when (error is ArgumentException or System.Xml.XmlException)
        {
            return Error(id, -32602, error.Message);
        }
    }

    public static object Error(JsonElement? id, int code, string message) => new { jsonrpc = "2.0", id, error = new { code, message } };

    private static XElement BuildCommand(string name, JsonElement arguments)
    {
        if (arguments.ValueKind is not (JsonValueKind.Undefined or JsonValueKind.Object))
            throw new ArgumentException("arguments must be an object");
        switch (name)
        {
            case "cqf_session_get":
                return new XElement("status");
            case "cqf_tools_find":
                return new XElement("discover", new XAttribute("search", OptionalString(arguments, "search")));
            case "cqf_tool_call":
                if (arguments.ValueKind != JsonValueKind.Object || !arguments.TryGetProperty("tool_arguments", out JsonElement values) || values.ValueKind != JsonValueKind.Object)
                    throw new ArgumentException("tool_arguments must be an object");
                string id = RequiredString(arguments, "request_id");
                string tool = RequiredString(arguments, "tool");
                return new XElement("call", new XAttribute("id", id), new XAttribute("name", tool),
                    new XElement("arguments", values.EnumerateObject().OrderBy(value => value.Name, StringComparer.Ordinal)
                        .Select(value => new XElement(XName.Get(System.Xml.XmlConvert.VerifyNCName(value.Name)),
                            value.Value.ValueKind == JsonValueKind.String ? value.Value.GetString() :
                            throw new ArgumentException("CQF tool arguments must be strings")))));
            default:
                throw new ArgumentException("Unknown tool");
        }
    }

    private async Task<XElement> CallGame(XElement command)
    {
        byte[] body = Encoding.UTF8.GetBytes(command.ToString(SaveOptions.DisableFormatting));
        if (body.Length > 524288) throw new ArgumentException("CQF request too large");
        using NamedPipeClientStream pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(3000);
        byte[] packet = new byte[4 + body.Length];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(packet, body.Length);
        body.CopyTo(packet, 4);
        using CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await pipe.WriteAsync(packet, timeout.Token);
        byte[] header = new byte[4];
        await pipe.ReadExactlyAsync(header, timeout.Token);
        int length = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(header);
        if (length is < 1 or > 4194304) throw new IOException("Invalid CQF reply length");
        byte[] reply = new byte[length];
        await pipe.ReadExactlyAsync(reply, timeout.Token);
        return XElement.Parse(Encoding.UTF8.GetString(reply));
    }

    private static string RequiredString(JsonElement source, string key)
    {
        string value = OptionalString(source, key);
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Missing " + key);
        return value;
    }

    private static string OptionalString(JsonElement source, string key)
    {
        if (source.ValueKind != JsonValueKind.Object || !source.TryGetProperty(key, out JsonElement value)) return "";
        if (value.ValueKind != JsonValueKind.String) throw new ArgumentException(key + " must be a string");
        return value.GetString()!;
    }

    private static object ToolResult(string text, bool error) => new { content = new[] { new { type = "text", text } }, isError = error };

    private static readonly JsonElement Tools = JsonDocument.Parse("""
        [
          {"name":"cqf_session_get","description":"Check connection, loaded game and edit permission.","inputSchema":{"type":"object","properties":{},"additionalProperties":false}},
          {"name":"cqf_tools_find","description":"Find current CQF editor tools by name or capability. Start with list_targets and select_target. Discover again after selecting a target.","inputSchema":{"type":"object","properties":{"search":{"type":"string"}},"additionalProperties":false}},
          {"name":"cqf_tool_call","description":"Call a discovered CQF tool. Use exact current IDs. Read and verify state around edits. For retries repeat the same request_id and arguments so writes execute once.","inputSchema":{"type":"object","properties":{"tool":{"type":"string"},"request_id":{"type":"string","maxLength":120},"tool_arguments":{"type":"object","additionalProperties":{"type":"string"}}},"required":["tool","request_id","tool_arguments"],"additionalProperties":false}}
        ]
        """).RootElement.Clone();

    private readonly string pipeName;
}
