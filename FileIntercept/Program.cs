using System.IO.Pipelines;
using System.Security.Claims;
using System.Text;
using FileIntercept;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(o => { o.DefaultScheme = "Cookies"; })
    .AddCookie(o => { o.LoginPath = "/signin"; });
builder.Services.AddAuthorization();
builder.Services.AddSingleton<Database>();
builder.Services.AddScoped<UserFileMiddleware>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
// this order is important
app.UseMiddleware<UserFileMiddleware>();
app.UseStaticFiles();

app.MapGet("/", (ClaimsPrincipal user) =>
{
    var name = user.Identity?.IsAuthenticated == true
        ? user.Identity.Name
        : "Not Authenticated";

    //lang=html
    var html =
    $"""
    <html>
        <body>
        <h1>Current User: {name}</h1>
        <p>Sign in first</p>
            <ul>
                <li>Slava : <a href="/signin/Slava">Sign in</a> or <a href="/file.html">Download File</a></li>
                <li>Fred : <a href="/signin/Fred">Sign in</a> or <a href="/file.html">Download File</a></li>
            </ul>
        </body>
    </html>
    """;

    return Results.File(
        Encoding.UTF8.GetBytes(html),
        "text/html"
    );
});

app.MapGet("/signin/{userId?}", async (HttpContext ctx, string? userId) =>
{
    userId ??= "Slava";

    var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
        new[]
        {
            new Claim(ClaimTypes.Name, userId),
        }, "Cookies"));

    await ctx.SignInAsync(claimsPrincipal);
    return Results.Redirect("/");
});

app.MapGet("/signout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync();
    return Results.Redirect("/");
});

app.Run();

public class UserFileMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var user = context.User.Identity;
        var db = context.RequestServices.GetRequiredService<Database>();
        
        if (user?.IsAuthenticated == true && 
            context.Request.Path == "/file.html")
        {
            var stream = db.GetFileByUserId(user?.Name!);

            if (stream is null)
            {
                context.Response.StatusCode = 404;
            }
            else
            {
                var text = await new StreamReader(stream).ReadToEndAsync();
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(text);
            }
        }
        else
        {
            await next(context);
        }
    }
}