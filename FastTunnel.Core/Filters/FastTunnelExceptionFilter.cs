// Licensed under the Apache License, Version 2.0 (the "License")

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FastTunnel.Core.Filters;

public class FastTunnelExceptionFilter(ILogger<FastTunnelExceptionFilter> logger, IWebHostEnvironment hostingEnvironment) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (!hostingEnvironment.IsDevelopment())
        {
            return;
        }

        logger.LogError(context.Exception, "[全局异常]");
    }
}
