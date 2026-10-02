using Microsoft.EntityFrameworkCore;
using TreeApp.DataLayer;
using TreeApp.Requests;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddDbContext<ApplicationDbContext>
(o => 
    o.UseSqlServer(builder.Configuration.GetConnectionString("Database"))
);
builder.Services.AddScoped<INodeRepository, NodeRepository>();

var app = builder.Build();

app.MapRazorPages();
app.UseStaticFiles();
app.UseDefaultFiles();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider
        .GetRequiredService<INodeRepository>()
        .EnsureDbSeeded(new CancellationToken());
}

app.MapGet
(
    "/api/nodes",
    (int? parentId, INodeRepository repo, CancellationToken ct) => 
        repo.GetChildrenAsync(parentId, ct)
);

app.MapGet
(
    "/api/nodes/{id:int}",
    async (int id, INodeRepository repo, CancellationToken ct) =>
        await repo.GetAsync(id, ct) is { } node ? Results.Ok(node) : Results.NotFound()
);

app.MapPost
(
    "/api/apply",
    async (ApplyRequest req, INodeRepository repo, CancellationToken ct) =>
    {
        try 
        { 
            var idMap = await repo.ApplyAsync(req, ct);
            return Results.Ok(new { idMap });
        }
        catch (Exception ex) 
        { 
            return Results.Conflict(new { error = ex.Message });
        }
    }
);

app.MapPost
(
    "/api/reset",
    async (INodeRepository repo, CancellationToken ct) =>
    {
        await repo.ResetAsync(ct);
        return Results.NoContent();
    }
);

app.Run();
