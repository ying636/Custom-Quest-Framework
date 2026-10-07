using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using QuestEditor_Library;

internal static class AIStreamingChecks
{
    public static async Task Run(CQFAIModel model, CQFAIResourceCatalog catalog)
    {
        List<CQFAIStreamUpdate> updates = new();
        CQFAIStreamResponse parser = new(updates.Add);
        parser.Append(Chunk(new { role = "assistant", content = (string?)null }));
        parser.Append(Chunk(new { reasoning_summary = "CQF_Check_Thinking" }));
        Check(updates.Last().Reasoning == "CQF_Check_Thinking", "reasoning progress is visible before completion without an empty role delta hiding it");
        parser.Append(Chunk(new { content = "你" }));
        parser.Append(Chunk(new { content = "好" }));
        parser.Append(Chunk(new { }, "stop")); parser.Append("[DONE]");
        Check(parser.Complete().Descendants("content").Single().Value == "你好" && updates.Last().Text == "你好", "stream accumulates UTF-8 reply fragments and publishes the complete preview");
        Reject(() => parser.Append(Chunk(new { content = "unexpected" })), "data after the terminal marker is rejected");
        CQFAIStreamResponse tools = new(_ => { });
        tools.Append(Chunk(new { tool_calls = new[] { new { index = 1, id = "CQF_Check_Second", type = "function", function = new { name = "cqf_get_", arguments = "{" } }, new { index = 0, id = "CQF_Check_First", type = "function", function = new { name = "cqf_get_", arguments = "{" } } } }));
        tools.Append(Chunk(new { tool_calls = new[] { new { index = 0, function = new { name = "context", arguments = "}" } }, new { index = 1, function = new { name = "context", arguments = "}" } } } }));
        tools.Append(Chunk(new { }, "tool_calls")); tools.Append("[DONE]");
        XElement[] calls = tools.Complete().Descendants("tool_calls").Single().Elements().ToArray();
        Check(calls.Select(call => CQFAIToolCall.FromJson(call).Id).SequenceEqual(new[] { "CQF_Check_First", "CQF_Check_Second" }), "interleaved tool fragments assemble by index and preserve call IDs");
        Check(calls.All(call => CQFAIToolCall.FromJson(call).Name == "cqf_get_context" && CQFAIToolCall.FromJson(call).Arguments.Elements().Count() == 0), "function names and JSON arguments can be split across chunks");
        foreach (string? finish in new string?[] { null, "length", "content_filter", "unknown" })
        {
            CQFAIStreamResponse incomplete = new(_ => { }); incomplete.Append(Chunk(new { content = "CQF_Check_Partial" }, finish)); incomplete.Append("[DONE]");
            Reject(() => incomplete.Complete(), "incomplete stream finish is rejected: " + finish);
        }
        CQFAIStreamResponse missingDone = new(_ => { }); missingDone.Append(Chunk(new { content = "CQF_Check_Partial" }, "stop"));
        Reject(() => missingDone.Complete(), "premature EOF without DONE is never treated as a complete operation");
        CQFAIStreamResponse wrongType = new(_ => { }); Reject(() => wrongType.Append(Chunk(new { content = 123 })), "stream content must be string or null");
        CQFAIStreamResponse late = new(_ => { }); late.Append(Chunk(new { }, "stop")); Reject(() => late.Append(Chunk(new { content = "CQF_Check_Late" })), "content after finish_reason is rejected");
        CQFAIStreamResponse badIndex = new(_ => { }); Reject(() => badIndex.Append(Chunk(new { tool_calls = new[] { new { index = 8 } } })), "stream tool count remains limited to eight");
        CQFAIStreamResponse sparse = new(_ => { }); sparse.Append(Chunk(new { tool_calls = new[] { new { index = 1, id = "CQF_Check_Sparse", type = "function", function = new { name = "cqf_get_context", arguments = "{}" } } } }));
        sparse.Append(Chunk(new { }, "tool_calls")); sparse.Append("[DONE]"); Reject(() => sparse.Complete(), "sparse tool indices are rejected before execution");
        CQFAIActivity activity = new(1);
        activity.Update(new CQFAIStreamUpdate("<assistant><reply>你&amp;我</reply><changes>secret tool XML", "CQF_Check_Reasoning", Array.Empty<string>()));
        Check(activity.Preview == "你&我" && activity.Reasoning == "CQF_Check_Reasoning", "XML preview shows only reply text and keeps reasoning separate from operation XML");
        activity.NextRound(); activity.Update(new CQFAIStreamUpdate("CQF_Check_Reply", "CQF_Check_NextReasoning", new[] { "cqf_apply_changes" }));
        Check(activity.Reasoning.Contains("CQF_Check_Reasoning") && activity.Reasoning.Contains("CQF_Check_NextReasoning"), "reasoning remains available across tool rounds");
        activity.Complete(); activity.Complete("CQF_AI_ActivityStopped");
        Check(activity.Finished && activity.Preview.Length == 0 && activity.StageKey == "CQF_AI_ActivityDone", "closing a completed activity never changes it into a cancelled task");
        AIFakeLiveMap map = new(model);
        CQFAIHarness harness = new(model, catalog, new CQFAIConversation(model, catalog), CQFAILiveMapContext.Create(map), "", true, false, true);
        List<string> execution = new(); harness.ToolProgress = (call, result) => execution.Add(call.Id + (result == null ? ":start" : ":" + result.Attribute("success")!.Value));
        using TcpListener probe = new(IPAddress.Loopback, 0); probe.Start(); int port = ((IPEndPoint)probe.LocalEndpoint).Port; probe.Stop();
        using HttpListener listener = new(); listener.Prefixes.Add("http://127.0.0.1:" + port + "/"); listener.Start();
        using CancellationTokenSource deadline = new(TimeSpan.FromSeconds(30));
        TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously), resume = new(TaskCreationOptions.RunContinuationsAsynchronously), cancellationReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
        string arguments = JsonSerializer.Serialize(new { changes_xml = "<changes><roof def='CQF_Check_Roof' x='4' z='4'/></changes>" });
        Task server = Task.Run(async () =>
        {
            for (int index = 0; index < 9; index++)
            {
                HttpListenerContext request = await listener.GetContextAsync().WaitAsync(deadline.Token);
                using JsonDocument payload = JsonDocument.Parse(await new StreamReader(request.Request.InputStream, Encoding.UTF8).ReadToEndAsync(deadline.Token));
                if (index == 6) Check(!payload.RootElement.GetProperty("stream").GetBoolean(), "explicit unsupported-stream response falls back to nonstream request");
                else Check(payload.RootElement.GetProperty("stream").GetBoolean(), "live UI requests ask the API for streaming");
                if (index == 4) Check(!payload.RootElement.TryGetProperty("stream_options", out _), "unsupported stream_options are omitted on retry");
                if (index == 3 || index == 5 || index == 7)
                {
                    request.Response.StatusCode = index == 7 ? 503 : 400;
                    await Json(request, new { error = index == 3 ? "stream_options unsupported" : index == 5 ? "stream unsupported" : "unavailable" }, deadline.Token);
                    continue;
                }
                if (index == 6)
                {
                    await Json(request, new { choices = new[] { new { finish_reason = "stop", message = new { content = "CQF_Check_Fallback", reasoning_summary = "CQF_Check_FallbackReasoning" } } } }, deadline.Token);
                    continue;
                }
                request.Response.ContentType = "text/event-stream"; request.Response.SendChunked = true;
                if (index == 0)
                {
                    Check(payload.RootElement.GetProperty("stream_options").GetProperty("include_usage").GetBoolean(), "stream requests opt into the final usage chunk");
                    await Event(request, Chunk(new { reasoning_summary = "CQF_Check_LiveReasoning" }), deadline.Token);
                    await Event(request, Chunk(new { tool_calls = new[] { new { index = 0, id = "CQF_Check_StreamWrite", type = "function", function = new { name = "cqf_apply_changes", arguments = arguments.Substring(0, 25) } } } }), deadline.Token);
                    ready.SetResult(); await resume.Task.WaitAsync(deadline.Token);
                    await Event(request, Chunk(new { tool_calls = new[] { new { index = 0, function = new { arguments = arguments.Substring(25) } } } }), deadline.Token);
                    await Event(request, Chunk(new { }, "tool_calls"), deadline.Token);
                    await Event(request, JsonSerializer.Serialize(new { choices = Array.Empty<object>(), usage = new { prompt_tokens = 100, completion_tokens = 10, total_tokens = 110 } }), deadline.Token);
                }
                else if (index == 1)
                {
                    await Event(request, Chunk(new { tool_calls = new[] { new { index = 0, id = "CQF_Check_Unfinished", type = "function", function = new { name = "cqf_apply_changes", arguments } } } }), deadline.Token);
                    request.Response.Close(); continue;
                }
                else if (index == 2)
                {
                    await Event(request, Chunk(new { reasoning_content = "CQF_Check_Cancellable" }), deadline.Token);
                    cancellationReady.SetResult(); await Task.Delay(250, deadline.Token); request.Response.Close(); continue;
                }
                else
                {
                    byte[] bytes = Encoding.UTF8.GetBytes("data: " + Chunk(new { content = "你好，正在回复。" }) + "\n\n");
                    int cut = Array.IndexOf(bytes, (byte)0xE4) + 1;
                    await request.Response.OutputStream.WriteAsync(bytes.AsMemory(0, cut), deadline.Token);
                    await request.Response.OutputStream.FlushAsync(deadline.Token);
                    await request.Response.OutputStream.WriteAsync(bytes.AsMemory(cut), deadline.Token);
                    await Event(request, Chunk(new { }, "stop"), deadline.Token);
                }
                await Event(request, "[DONE]", deadline.Token); request.Response.Close();
            }
        }, deadline.Token);
        CQFDialogAIClient client = new("http://127.0.0.1:" + port + "/v1", "CQF_Check_StreamModel", "CQF_Check_StreamKey", 10);
        Task<string> pending = client.CompleteConversationAsync(harness.Instructions, new[] { new CQFAIMessage("user", "CQF_Check_Request") }, deadline.Token, harness.Registry.Tools, true);
        await ready.Task.WaitAsync(deadline.Token);
        for (int attempt = 0; attempt < 50 && client.LastProgress?.Reasoning != "CQF_Check_LiveReasoning"; attempt++) await Task.Delay(10, deadline.Token);
        Check(!pending.IsCompleted && client.LastProgress?.Reasoning == "CQF_Check_LiveReasoning" && map.Cells.Count == 0, "HTTP stream reports reasoning while incomplete tools have no map effects");
        resume.SetResult();
        Check(harness.Process(await pending.WaitAsync(deadline.Token)) && map.Cells["roof:4:4"] == "CQF_Check_Roof", "assembled stream tools execute only after the complete valid response");
        Check(client.LastUsage?.Total == 110 && execution.SequenceEqual(new[] { "CQF_Check_StreamWrite:start", "CQF_Check_StreamWrite:true" }), "stream final usage and real tool execution events are preserved");
        try { await client.CompleteConversationAsync("", Array.Empty<CQFAIMessage>(), deadline.Token, harness.Registry.Tools, true); throw new InvalidOperationException("unfinished stream accepted"); }
        catch (InvalidOperationException error) when (error.Message == "CQF_DialogAI_Incomplete") { Check(map.Cells.Count == 1, "disconnect before completion executes no additional map changes"); }
        using CancellationTokenSource cancel = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);
        Task<string> cancelled = client.CompleteConversationAsync("", Array.Empty<CQFAIMessage>(), cancel.Token, harness.Registry.Tools, true);
        await cancellationReady.Task.WaitAsync(deadline.Token); cancel.Cancel();
        try { await cancelled.WaitAsync(TimeSpan.FromSeconds(3)); throw new InvalidOperationException("stream cancellation ignored"); }
        catch (OperationCanceledException) { Check(true, "cancellation interrupts a pending body read without waiting for EOF"); }
        string compatible = await client.CompleteConversationAsync("", Array.Empty<CQFAIMessage>(), deadline.Token, harness.Registry.Tools, true);
        Check(compatible.Contains("你好") && client.LastUsage == null, "stream-options fallback preserves Unicode content and missing usage");
        string buffered = await client.CompleteConversationAsync("", Array.Empty<CQFAIMessage>(), deadline.Token, harness.Registry.Tools, true);
        Check(buffered.Contains("CQF_Check_Fallback") && client.LastProgress?.Reasoning == "CQF_Check_FallbackReasoning", "JSON fallback preserves replies and explicitly returned reasoning");
        client = new("http://127.0.0.1:" + port + "/v1", "CQF_Check_StreamModel", "CQF_Check_StreamKey", 10);
        try { await client.CompleteConversationAsync("", Array.Empty<CQFAIMessage>(), deadline.Token, harness.Registry.Tools, true); throw new InvalidOperationException("HTTP error accepted"); }
        catch (InvalidOperationException error) when (error.Message == "CQF_DialogAI_HTTP 503") { Check(!client.ReceivedResponse && client.LastUsage == null, "HTTP errors are not retried as unsupported streaming or counted as valid usage"); }
        Check((await client.CompleteConversationAsync("", Array.Empty<CQFAIMessage>(), deadline.Token, harness.Registry.Tools, true)).Contains("你好"), "UTF-8 characters split between network writes decode correctly");
        await server.WaitAsync(deadline.Token);
        harness.Transaction!.Undo(); Check(map.Cells.Count == 0, "streaming execution retains task undo");
    }
    private static string Chunk(object delta, string? finish = null) => JsonSerializer.Serialize(new { choices = new[] { new { index = 0, delta, finish_reason = finish } } }, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    private static async Task Event(HttpListenerContext request, string data, CancellationToken token)
    {
        await request.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("data: " + data + "\n\n"), token);
        await request.Response.OutputStream.FlushAsync(token);
    }
    private static async Task Json(HttpListenerContext request, object value, CancellationToken token)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
        request.Response.ContentType = "application/json"; request.Response.ContentLength64 = bytes.Length;
        await request.Response.OutputStream.WriteAsync(bytes, token); request.Response.Close();
    }
    private static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
    private static void Reject(Action action, string name)
    {
        try { action(); } catch (Exception error) when (error is InvalidDataException || error is InvalidOperationException) { Console.WriteLine("PASS " + name); return; }
        throw new InvalidOperationException("Expected rejection: " + name);
    }
}
