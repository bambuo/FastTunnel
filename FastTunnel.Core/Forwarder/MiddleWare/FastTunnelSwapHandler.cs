using System;
using System.Threading;
using System.Threading.Tasks;
using FastTunnel.Core.Client;
using FastTunnel.Core.Extensions;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FastTunnel.Core.Forwarder.MiddleWare;

public class FastTunnelSwapHandler(ILogger<FastTunnelClientHandler> logger, FastTunnelServer fastTunnelServer)
{
    private static int _connectionCount;

    public static int ConnectionCount => _connectionCount;

    public async Task Handle(HttpContext context, Func<Task> next)
    {
        Interlocked.Increment(ref _connectionCount);

        try
        {
            if (context.Request.Method != "PROXY")
            {
                await next();
                return;
            }

            var requestId = context.Request.Path.Value?.Trim('/');
            logger.LogDebug($"[PROXY]:Start {requestId}");

            if (!fastTunnelServer.ResponseTasks.TryRemove(requestId, out var responseAwaiter))
            {
                logger.LogError($"[PROXY]:RequestId不存在 {requestId}");
                return;
            }

            var lifetime = context.Features.Get<IConnectionLifetimeFeature>();
            var transport = context.Features.Get<IConnectionTransportFeature>();

            if (lifetime == null || transport == null)
            {
                return;
            }

            await using var reverseConnection = new WebSocketStream(lifetime, transport);
            responseAwaiter.Item1.TrySetResult(reverseConnection);

            CancellationTokenSource cts;
            cts = responseAwaiter.Item2 != CancellationToken.None ? CancellationTokenSource.CreateLinkedTokenSource(lifetime.ConnectionClosed, responseAwaiter.Item2) : CancellationTokenSource.CreateLinkedTokenSource(lifetime.ConnectionClosed);

            var closedAwaiter = new TaskCompletionSource<object>();

            //lifetime.ConnectionClosed.Register((task) =>
            //{
            //    (task as TaskCompletionSource<object>).SetResult(null);
            //}, closedAwaiter);

            await closedAwaiter.Task.WaitAsync(cts.Token);
            logger.LogDebug($"[PROXY]:Closed {requestId}");
        }
        catch (TaskCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
        }
        finally
        {
            Interlocked.Decrement(ref _connectionCount);
            logger.LogDebug($"统计SWAP连接数：{ConnectionCount}");
        }
    }
}
