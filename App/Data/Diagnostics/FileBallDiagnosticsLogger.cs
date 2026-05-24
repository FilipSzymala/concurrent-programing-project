using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;

namespace Data.Diagnostics
{
    internal sealed class FileBallDiagnosticsLogger : BallDiagnosticsLogger
    {
        private const int WriterTakeTimeoutMs = 50;

        private readonly string _filePath;
        private readonly int _capacity;
        private readonly object _lifecycleLock = new object();

        private BlockingCollection<BallDiagnosticEntry> _queue;
        private Thread _writerThread;
        private CancellationTokenSource _cts;
        private StreamWriter _writer;
        private long _written;
        private long _dropped;
        private bool _running;

        public FileBallDiagnosticsLogger(string filePath, int capacity)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path must be provided.", nameof(filePath));
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be at least 1.");

            _filePath = filePath;
            _capacity = capacity;
        }

        public override long WrittenCount => Interlocked.Read(ref _written);
        public override long DroppedCount => Interlocked.Read(ref _dropped);
        public override int QueueCapacity => _capacity;

        public override void Start()
        {
            lock (_lifecycleLock)
            {
                if (_running) return;

                string directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                _queue = new BlockingCollection<BallDiagnosticEntry>(
                    new ConcurrentQueue<BallDiagnosticEntry>(), _capacity);
                _cts = new CancellationTokenSource();
                _writer = new StreamWriter(
                    new FileStream(_filePath, FileMode.Append, FileAccess.Write, FileShare.Read),
                    new UTF8Encoding(false));
                _writer.WriteLine("# ticks;id;x;y;vx;vy;step");

                CancellationToken token = _cts.Token;
                BlockingCollection<BallDiagnosticEntry> queue = _queue;
                StreamWriter writer = _writer;

                _writerThread = new Thread(() => WriterLoop(queue, writer, token))
                {
                    IsBackground = true,
                    Name = "BallDiagnosticsWriter"
                };
                _writerThread.Start();
                _running = true;
            }
        }

        public override void Stop()
        {
            BlockingCollection<BallDiagnosticEntry> queue;
            Thread thread;
            CancellationTokenSource cts;
            StreamWriter writer;

            lock (_lifecycleLock)
            {
                if (!_running) return;
                queue = _queue;
                thread = _writerThread;
                cts = _cts;
                writer = _writer;
                _queue = null;
                _writerThread = null;
                _cts = null;
                _writer = null;
                _running = false;
            }

            try { queue?.CompleteAdding(); } catch (ObjectDisposedException) { }
            thread?.Join(TimeSpan.FromSeconds(2));
            if (thread != null && thread.IsAlive)
                cts?.Cancel();
            thread?.Join(TimeSpan.FromSeconds(1));

            try { writer?.Flush(); } catch { }
            try { writer?.Dispose(); } catch { }
            try { queue?.Dispose(); } catch { }
            try { cts?.Dispose(); } catch { }
        }

        public override bool Log(BallDiagnosticEntry entry)
        {
            BlockingCollection<BallDiagnosticEntry> queue = _queue;
            if (queue == null || queue.IsAddingCompleted)
            {
                Interlocked.Increment(ref _dropped);
                return false;
            }

            try
            {
                if (queue.TryAdd(entry))
                    return true;

                Interlocked.Increment(ref _dropped);
                return false;
            }
            catch (ObjectDisposedException)
            {
                Interlocked.Increment(ref _dropped);
                return false;
            }
            catch (InvalidOperationException)
            {
                Interlocked.Increment(ref _dropped);
                return false;
            }
        }

        public override void Dispose() => Stop();

        private void WriterLoop(
            BlockingCollection<BallDiagnosticEntry> queue,
            StreamWriter writer,
            CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    BallDiagnosticEntry entry;
                    try
                    {
                        if (!queue.TryTake(out entry, WriterTakeTimeoutMs, token))
                        {
                            if (queue.IsCompleted) break;
                            continue;
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch (ObjectDisposedException) { break; }

                    try
                    {
                        writer.WriteLine(entry.ToAsciiLine());
                        Interlocked.Increment(ref _written);
                    }
                    catch (IOException) { }
                }

                try { writer.Flush(); } catch { }
            }
            catch (Exception) { }
        }
    }
}
