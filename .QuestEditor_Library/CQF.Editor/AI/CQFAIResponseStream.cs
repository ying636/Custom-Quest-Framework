using System.Threading;

namespace QuestEditor_Library
{
    public sealed class CQFAIResponseStream : Stream
    {
        public CQFAIResponseStream(Stream input, CancellationTokenSource deadline, TimeSpan idleTimeout)
        {
            this.input = input; this.deadline = deadline; this.idleTimeout = idleTimeout;
        }

        public override bool CanRead => input.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int received = input.Read(buffer, offset, count);
            if (received > 0) deadline.CancelAfter(idleTimeout);
            return received;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellation)
        {
            int received = await input.ReadAsync(buffer, offset, count, cancellation).ConfigureAwait(false);
            if (received > 0) deadline.CancelAfter(idleTimeout);
            return received;
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) input.Dispose();
            base.Dispose(disposing);
        }

        private readonly Stream input;
        private readonly CancellationTokenSource deadline;
        private readonly TimeSpan idleTimeout;
    }
}
