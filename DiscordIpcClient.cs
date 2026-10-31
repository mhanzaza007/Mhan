using System.Buffers.Binary;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace AppleMusicDiscordPresence;

internal sealed class DiscordIpcClient : IAsyncDisposable
{
    private NamedPipeClientStream? _pipe;
    private string? _activeTrackKey;
    private long? _activeStartTimestamp;

    public bool IsConnected => _pipe?.IsConnected == true;

    public async Task ConnectAsync(string applicationId, CancellationToken cancellationToken)
    {
        if (IsConnected) return;

        await DisposePipeAsync();
        Exception? lastError = null;

        for (var index = 0; index <= 9; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var candidate = new NamedPipeClientStream(".", $"discord-ipc-{index}", PipeDirection.InOut, PipeOptions.Asynchronous);
            try
            {
                await candidate.ConnectAsync(350, cancellationToken);
                _pipe = candidate;
                await WritePacketAsync(0, JsonSerializer.SerializeToUtf8Bytes(new { v = 1, client_id = applicationId }), cancellationToken);

                using var readyTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                readyTimeout.CancelAfter(TimeSpan.FromSeconds(5));
                var ready = await ReadPacketAsync(readyTimeout.Token);
                using var json = JsonDocument.Parse(ready.Payload);
                if (ready.Opcode != 1 || !json.RootElement.TryGetProperty("evt", out var evt) || evt.GetString() != "READY")
                    throw new IOException("Discord ตอบกลับ handshake ไม่ถูกต้อง");

                return;
            }
            catch (Exception ex) when (ex is IOException or TimeoutException or OperationCanceledException or JsonException)
            {
                lastError = ex;
                if (ReferenceEquals(_pipe, candidate)) _pipe = null;
                candidate.Dispose();
                if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested) throw;
            }
        }

        throw new IOException("เชื่อมต่อ Discord ไม่ได้ กรุณาเปิด Discord Desktop", lastError);
    }

    public Task SetTrackAsync(string applicationId, TrackInfo track, CancellationToken cancellationToken)
    {
        var activity = new
        {
            type = 2,
            name = track.ServiceName,
            details = track.Title,
            state = track.PreviewLyric ?? track.Artist,
            timestamps = track.IsPlaying ? new { start = GetActivityStartTimestamp(track) } : null,
            assets = track.ArtworkUrl is null ? null : new
            {
                large_image = track.ArtworkUrl,
                large_text = $"{track.Title} — {track.Artist}",
                large_url = track.TrackUrl
            }
        };
        return SendActivityAsync(applicationId, activity, cancellationToken);
    }

    public Task ClearActivityAsync(string applicationId, CancellationToken cancellationToken)
    {
        _activeTrackKey = null;
        _activeStartTimestamp = null;
        return SendActivityAsync(applicationId, null, cancellationToken);
    }

    private long GetActivityStartTimestamp(TrackInfo track)
    {
        var trackKey = $"{track.ServiceName}\u001f{track.Title}\u001f{track.Artist}";
        var positionSeconds = (long)Math.Clamp(Math.Floor(track.Position.TotalSeconds), 0, 86400);
        var candidateStart = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - positionSeconds;
        if (_activeTrackKey != trackKey || _activeStartTimestamp is null
            || Math.Abs(candidateStart - _activeStartTimestamp.Value) > 3)
        {
            _activeTrackKey = trackKey;
            _activeStartTimestamp = candidateStart;
        }
        return _activeStartTimestamp.Value;
    }

    private async Task SendActivityAsync(string applicationId, object? activity, CancellationToken cancellationToken)
    {
        if (!IsConnected) await ConnectAsync(applicationId, cancellationToken);

        var payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            cmd = "SET_ACTIVITY",
            args = new { pid = Environment.ProcessId, activity },
            nonce = Guid.NewGuid().ToString("D")
        });
        await WritePacketAsync(1, payload, cancellationToken);
    }

    private async Task WritePacketAsync(int opcode, byte[] payload, CancellationToken cancellationToken)
    {
        if (_pipe is null || !IsConnected) throw new IOException("การเชื่อมต่อกับ Discord หลุดแล้ว");

        var header = new byte[8];
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(0, 4), opcode);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(4, 4), payload.Length);
        await _pipe.WriteAsync(header, cancellationToken);
        await _pipe.WriteAsync(payload, cancellationToken);
        await _pipe.FlushAsync(cancellationToken);
    }

    private async Task<(int Opcode, byte[] Payload)> ReadPacketAsync(CancellationToken cancellationToken)
    {
        if (_pipe is null) throw new IOException("ไม่มีการเชื่อมต่อกับ Discord");

        var header = new byte[8];
        await ReadExactlyAsync(_pipe, header, cancellationToken);
        var opcode = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(0, 4));
        var length = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4, 4));
        if (length < 0 || length > 1024 * 1024) throw new IOException("ข้อมูลจาก Discord มีขนาดไม่ถูกต้อง");

        var payload = new byte[length];
        await ReadExactlyAsync(_pipe, payload, cancellationToken);
        return (opcode, payload);
    }

    private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var count = await stream.ReadAsync(buffer.AsMemory(offset), cancellationToken);
            if (count == 0) throw new EndOfStreamException("Discord ปิดการเชื่อมต่อ");
            offset += count;
        }
    }

    private async Task DisposePipeAsync()
    {
        if (_pipe is null) return;
        try { await _pipe.DisposeAsync(); }
        catch { _pipe.Dispose(); }
        _pipe = null;
    }

    public ValueTask DisposeAsync() => new(DisposePipeAsync());
}
