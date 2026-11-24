#if RIMWORLD_1_7
using System.IO.Pipes;
using System.Text;
using System.Xml.Linq;

namespace QuestEditor_Library
{
    internal sealed class CQFAIExternalPipe : IDisposable
    {
        public CQFAIExternalPipe()
        {
            Name = "CQF-" + Guid.NewGuid().ToString("N");
            Completion = Listen(stop.Token);
        }

        public string Name { get; }
        public Task Completion { get; }
        public (XElement Request, TaskCompletionSource<XElement> Reply)? Pending { get; private set; }

        public void Dispose()
        {
            stop.Cancel();
            lock (gate) Pending?.Reply.TrySetCanceled();
        }

        public (XElement Request, TaskCompletionSource<XElement> Reply)? Take()
        {
            lock (gate)
            {
                var request = Pending;
                Pending = null;
                return request;
            }
        }

        private async Task Listen(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                using NamedPipeServerStream pipe = new NamedPipeServerStream(Name, PipeDirection.InOut, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(cancellation).ConfigureAwait(false);
                try
                {
                    using CancellationTokenSource inputDeadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
                    inputDeadline.CancelAfter(TimeSpan.FromSeconds(10));
                    byte[] header = new byte[4];
                    await pipe.ReadExactlyAsync(header, inputDeadline.Token).ConfigureAwait(false);
                    int length = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(header);
                    if (length is < 1 or > 524288) throw new InvalidDataException("CQF_AI_InvalidTool");
                    byte[] body = new byte[length];
                    await pipe.ReadExactlyAsync(body, inputDeadline.Token).ConfigureAwait(false);
                    XElement request = CQFAIChanges.Parse(Encoding.UTF8.GetString(body));
                    TaskCompletionSource<XElement> reply = new TaskCompletionSource<XElement>(TaskCreationOptions.RunContinuationsAsynchronously);
                    lock (gate)
                    {
                        cancellation.ThrowIfCancellationRequested();
                        Pending = (request, reply);
                    }

                    XElement result = await reply.Task.WaitAsync(cancellation).ConfigureAwait(false);
                    byte[] response = Encoding.UTF8.GetBytes(result.ToString(SaveOptions.DisableFormatting));
                    if (response.Length > 4194304)
                        response = Encoding.UTF8.GetBytes("<error>CQF_AI_ContextTooLarge</error>");
                    System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(header, response.Length);
                    using CancellationTokenSource outputDeadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
                    outputDeadline.CancelAfter(TimeSpan.FromSeconds(10));
                    await pipe.WriteAsync(header, outputDeadline.Token).ConfigureAwait(false);
                    await pipe.WriteAsync(response, outputDeadline.Token).ConfigureAwait(false);
                }
                catch (Exception error) when (!cancellation.IsCancellationRequested &&
                    error is IOException or InvalidDataException or OperationCanceledException or System.Xml.XmlException)
                {
                    Verse.Log.Warning("CQF MCP: " + error.Message);
                }
            }
        }

        private readonly CancellationTokenSource stop = new CancellationTokenSource();
        private readonly object gate = new object();
    }
}
#endif
