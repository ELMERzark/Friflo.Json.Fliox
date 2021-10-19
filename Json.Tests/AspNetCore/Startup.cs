using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Remote;
using Friflo.Json.Fliox.Mapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Tests.Main
{
	public class Startup
	{
		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
            var database        = new MemoryDatabase();
            var hostDatabase    = new HttpHostDatabase (database);
            hostDatabase.requestHandler = new RequestHandler("./Json.Tests/www");   // optional. Used to serve static web content

			app.UseRouting();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapGet("hello/", async context =>
				{
					await context.Response.WriteAsync("Hello World!");
				});
                
                endpoints.Map("/{*path}", async context => {
                    var req = context.Request;
                    var reqCtx = new RequestContext(req.Path.Value, req.Method, req.Body);
                    await hostDatabase.ExecuteHttpRequest(reqCtx).ConfigureAwait(false);
                    
                    var resp = context.Response;
                    resp.StatusCode     = (int)reqCtx.Status;
                    resp.ContentType    = reqCtx.ResponseContentType;
                    resp.ContentLength  = reqCtx.Response.Length;
                    await resp.Body.WriteAsync(reqCtx.Response, 0, reqCtx.Response.Length).ConfigureAwait(false);
                });
            });
		}
	}
}
