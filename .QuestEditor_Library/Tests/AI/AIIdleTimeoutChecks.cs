using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using QuestEditor_Library;

internal static class AIIdleTimeoutChecks
{
    public static async Task Run()
    {
        using TcpListener probe = new(IPAddress.Loopback, 0);
        probe.Start(); int port = ((IPEndPoint)probe.LocalEndpoint).Port; probe.Stop();
        using HttpListener listener = new(); listener.Prefixes.Add("http://127.0.0.1:" + port + "/"); listener.Start();
        using CancellationTokenSource deadline = new(TimeSpan.FromSeconds(45));
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task server = Task.Run(async () =>
        {
            HttpListenerContext request = await listener.GetContextAsync().WaitAsync(deadline.Token);
            await new StreamReader(request.Request.InputStream).ReadToEndAsync(deadline.Token);
            request.Response.ContentType = "text/event-stream"; request.Response.SendChunked = true;
            await Write(request, "data: {\"choices\":[{\"index\":0,\"delta\":{\"reasoning_summary\":\"CQF_Check_StillWorking\"}}]}\n\n", deadline.Token);
            for (int index = 0; index < 3; index++)
            {
                await Task.Delay(3600, deadline.Token);
                await Write(request, ": heartbeat\n\n", deadline.Token);
            }
            await Write(request, "data: {\"choices\":[{\"index\":0,\"delta\":{\"content\":\"CQF_Check_LongReply\"},\"finish_reason\":\"stop\"}]}\n\ndata: [DONE]\n\n", deadline.Token);
            request.Response.Close();
            request = await listener.GetContextAsync().WaitAsync(deadline.Token);
            await new StreamReader(request.Request.InputStream).ReadToEndAsync(deadline.Token);
            request.Response.ContentType = "text/event-stream"; request.Response.SendChunked = true;
            await Write(request, "data: {\"choices\":[{\"index\":0,\"delta\":{\"reasoning_summary\":\"CQF_Check_LastFeedback\"}}]}\n\n", deadline.Token);
            await release.Task.WaitAsync(deadline.Token);
            request.Response.Close();
        }, deadline.Token);
        CQFDialogAIClient client = new("http://127.0.0.1:" + port + "/v1", "CQF_Check_IdleModel", "", 10);
        Stopwatch elapsed = Stopwatch.StartNew();
        string reply = await client.CompleteConversationAsync("", Array.Empty<CQFAIMessage>(), deadline.Token, stream: true);
        Check(elapsed.Elapsed.TotalSeconds > 10 && reply.Contains("CQF_Check_LongReply"), "stream heartbeats keep a request alive beyond its configured idle timeout");
        elapsed.Restart();
        try
        {
            await client.CompleteConversationAsync("", Array.Empty<CQFAIMessage>(), deadline.Token, stream: true);
            throw new InvalidOperationException("silent stream did not time out");
        }
        catch (OperationCanceledException)
        {
            Check(elapsed.Elapsed.TotalSeconds >= 9 && elapsed.Elapsed.TotalSeconds < 15 && client.LastProgress?.Reasoning == "CQF_Check_LastFeedback",
                "a stream stops only after a continuous interval without feedback and preserves its last progress");
        }
        finally { release.TrySetResult(); }
        await server.WaitAsync(deadline.Token);
        using CancellationTokenSource idle = new(); idle.CancelAfter(250);
        using MemoryStream bytes = new(Encoding.UTF8.GetBytes("CQF_Check_Fragment"));
        using CQFAIResponseStream monitored = new(bytes, idle, TimeSpan.FromMilliseconds(250));
        byte[] buffer = new byte[1];
        for (int index = 0; index < 5; index++)
        {
            await Task.Delay(100, deadline.Token);
            Check(await monitored.ReadAsync(buffer, 0, 1, deadline.Token) == 1 && !idle.IsCancellationRequested,
                "partial incoming bytes reset inactivity before a whole SSE line or UTF-8 token is available: " + index);
        }
    }

    private static async Task Write(HttpListenerContext request, string data, CancellationToken token)
    {
        await request.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(data), token);
        await request.Response.OutputStream.FlushAsync(token);
    }

    private static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
}
