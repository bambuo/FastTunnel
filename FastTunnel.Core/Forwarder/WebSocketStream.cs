using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections.Features;

namespace FastTunnel.Core.Forwarder;

internal sealed class WebSocketStream : Stream
{
    private readonly IConnectionLifetimeFeature _lifetimeFeature;
    private readonly Stream _readStream;
    private readonly Stream _wirteStream;

    public WebSocketStream(IConnectionLifetimeFeature lifetimeFeature, IConnectionTransportFeature transportFeature)
    {
        _readStream = transportFeature.Transport.Input.AsStream();
        _wirteStream = transportFeature.Transport.Output.AsStream();
        this._lifetimeFeature = lifetimeFeature;
    }

    public WebSocketStream(Stream stream)
    {
        _readStream = stream;
        _wirteStream = stream;
        _lifetimeFeature = null;
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
        _wirteStream.Flush();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return _wirteStream.FlushAsync(cancellationToken);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return _readStream.Read(buffer, offset, count);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _wirteStream.Write(buffer, offset, count);
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return _readStream.ReadAsync(buffer, cancellationToken);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _readStream.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        _wirteStream.Write(buffer);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _wirteStream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await _wirteStream.WriteAsync(buffer, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        _lifetimeFeature?.Abort();
    }

    public override ValueTask DisposeAsync()
    {
        _lifetimeFeature?.Abort();
        return ValueTask.CompletedTask;
    }
}
