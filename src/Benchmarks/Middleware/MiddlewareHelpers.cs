﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Benchmarks.Data;
using Fluid;
using Microsoft.AspNetCore.Http;

namespace Benchmarks.Middleware
{
    public static class MiddlewareHelpers
    {
        private const string FortunesTemplate = @"
<!DOCTYPE html>
<html>
<head><title>Fortunes</title></head>
<body>
    {{ fortunes.size }}
    <table>
        <tr><th>id</th><th>message</th></tr>
        {% for item in fortunes %}
            <tr><td>{{ item.Id }}</td><td>{{ item.Message }}</td></tr>
        {% endfor %}
    </table>
</body>
</html>
";

        private static FluidTemplate Template = FluidTemplate.Parse(FortunesTemplate);

        static MiddlewareHelpers()
        {
            TemplateContext.GlobalMemberAccessStrategy.Register<Fortune>();
        }

        public static int GetMultipleQueriesQueryCount(HttpContext httpContext)
        {
            var queries = 1;
            var queriesRaw = httpContext.Request.Query["queries"];

            if (queriesRaw.Count == 1)
            {
                int.TryParse(queriesRaw, out queries);
            }

            return queries > 500
                ? 500
                : queries > 0
                    ? queries
                    : 1;
        }

        public static Task RenderFortunesHtml2(IEnumerable<Fortune> model, HttpContext httpContext, HtmlEncoder htmlEncoder)
        {
            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.Response.ContentType = "text/html; charset=UTF-8";

            var sb = StringBuilderCache.Acquire();
            sb.Append("<!DOCTYPE html><html><head><title>Fortunes</title></head><body><table><tr><th>id</th><th>message</th></tr>");
            foreach (var item in model)
            {
                sb.Append("<tr><td>");
                sb.Append(item.Id.ToString(CultureInfo.InvariantCulture));
                sb.Append("</td><td>");
                sb.Append(htmlEncoder.Encode(item.Message));
                sb.Append("</td></tr>");
            }

            sb.Append("</table></body></html>");
            var response = StringBuilderCache.GetStringAndRelease(sb);
            // fortunes includes multibyte characters so response.Length is incorrect
            httpContext.Response.ContentLength = Encoding.UTF8.GetByteCount(response);
            return httpContext.Response.WriteAsync(response);
        }

        public static async Task RenderFortunesHtml(IEnumerable<Fortune> model, HttpContext httpContext, HtmlEncoder htmlEncoder)
        {
            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.Response.ContentType = "text/html; charset=UTF-8";

            var templateContext = new TemplateContext();
            templateContext.SetValue("fortunes", model);

            var response = await Template.RenderAsync(templateContext, htmlEncoder);

            // fortunes includes multibyte characters so response.Length is incorrect
            httpContext.Response.ContentLength = Encoding.UTF8.GetByteCount(response);
            await httpContext.Response.WriteAsync(response);
        }
    }
}
