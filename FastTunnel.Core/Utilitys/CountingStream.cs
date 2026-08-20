// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FastTunnel.Core.Models;

namespace FastTunnel.Core.Utilitys;

/// <summary>
///     包装流：Read/Write 的数据量累计到 TrafficStats（双向合计，按 Token）。
///     用于 TCP 隧道流与 Web 隧道流的数据桥接处计数。
/// </summary>
public sealed class CountingStream : Stream
{
    private readonly Stream _inner;
    private readonly TrafficStats _traffic;
    private readonly string _token;
    private bool _disposed;

    public CountingStream(Stream inner, TrafficStats traffic, string token)
    {
        _inner = inner;
        _traffic = traffic;
        _token = token;
    }

    public override bool CanRead => _inner.CanRead;

    public override bool CanSeek => _inner.CanSeek;

    public override bool CanWrite => _inner.CanWrite;

    public override long Length => _inner.Length;

    public override long Position
    {
        get => _inner.Position;
        set => _inner.Position = value;
    }

    public override void Flush() => _inner.Flush();

    public override int Read(byte[] buffer, int offset, int count)
    {
        var n = _inner.Read(buffer, offset, count);
        if (n > 0) _traffic.Add(_token, n);
        return n;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _inner.Write(buffer, offset, count);
        if (count > 0) _traffic.Add(_token, count);
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var n = await _inner.ReadAsync(buffer, cancellationToken);
        if (n > 0) _traffic.Add(_token, n);
        return n;
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await _inner.WriteAsync(buffer, cancellationToken);
        if (buffer.Length > 0) _traffic.Add(_token, buffer.Length);
    }

    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

    public override void SetLength(long value) => _inner.SetLength(value);

    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;
        if (disposing)
        {
            _inner.Dispose();
        }
        base.Dispose(disposing);
    }
}
