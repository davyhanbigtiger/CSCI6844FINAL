using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddOcelot(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// ✅ 改动：UseCors 移到这里，是第一个中间件
app.UseCors();

app.UseSwagger();
app.UseSwaggerUI();

app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api/aggregate"),
    appBuilder => appBuilder.UseRouting().UseEndpoints(e => e.MapControllers())
);

await app.UseOcelot();

app.Run();