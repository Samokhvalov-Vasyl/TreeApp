using Microsoft.EntityFrameworkCore;
using TreeApp.Requests;

namespace TreeApp.DataLayer;

public interface INodeRepository
{
    Task<Node?> GetAsync(int id, CancellationToken cancellationToken);
    Task<List<Node>> GetChildrenAsync(int? id, CancellationToken cancellationToken);
    Task<Dictionary<int, int>> ApplyAsync
    (
        ApplyRequest applyRequest,
        CancellationToken cancellationToken
    );
    Task ResetAsync(CancellationToken cancellationToken);
    Task EnsureDbSeeded(CancellationToken cancellationToken);
}

public class NodeRepository(ApplicationDbContext context) : INodeRepository
{
    public async Task<Dictionary<int, int>> ApplyAsync
    (
        ApplyRequest applyRequest,
        CancellationToken cancellationToken
    )
    {
        await using var transaction = await context.Database
            .BeginTransactionAsync(cancellationToken);

        var deletedIds = await GetSubtreeIdsAsync(applyRequest.Deletes, cancellationToken);
        if (deletedIds.Count > 0)
        {
            await context.Nodes
                .Where(x => deletedIds.Contains(x.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        var updated = applyRequest.Updates.Where(x => !deletedIds.Contains(x.Id)).ToList();
        foreach (var node in updated)
        {
            var existing = await context.Nodes
                .Where(x => x.Id == node.Id)
                .ExecuteUpdateAsync
                (x =>
                    x.SetProperty(n => n.Value, node.Value),
                    cancellationToken
                );
        }

        var createdNodes = new Dictionary<int, Node>();
        foreach (var item in applyRequest.Adds)
        {
            var newNode = new Node { Value = item.Value };
            if (item.ParentId < 0)
            {
                if (!createdNodes.TryGetValue(item.ParentId, out var parentNode))
                {
                    throw new Exception($"Unknown temp parent {item.ParentId}.");
                }
                newNode.Parent = parentNode;
            }
            else
            {
                if (!await context.Nodes.AnyAsync(x => x.Id == item.ParentId, cancellationToken))
                {
                    throw new Exception($"Parent node {item.ParentId} does not exist.");
                }
                newNode.ParentId = item.ParentId;
            }
            createdNodes[item.TempId] = newNode;
            context.Nodes.Add(newNode);
        }

        await context.SaveChangesAsync(cancellationToken);
        context.ChangeTracker.Clear();

        var idMap = createdNodes.ToDictionary(x => x.Key, x => x.Value.Id);
        await transaction.CommitAsync(cancellationToken);
        return idMap;
    }

    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database
            .BeginTransactionAsync(cancellationToken);

        await context.Nodes.ExecuteDeleteAsync(cancellationToken);
        await SeedAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        context.ChangeTracker.Clear();
    }

    public async Task<Node?> GetAsync(int id, CancellationToken cancellationToken)
    {
        return await context.Nodes.AsNoTracking()
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<Node>> GetChildrenAsync(int? id, CancellationToken cancellationToken)
    {
        return await context.Nodes.AsNoTracking()
            .Where(x => x.ParentId == id)
            .ToListAsync(cancellationToken);
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await context.AddRangeAsync
        (
            [
                new Node
                {
                    Value = 10,
                    Children =
                    [
                        new Node
                        {
                            Value = 20,
                            Children =
                            [
                                new Node { Value = 30 },
                                new Node
                                {
                                    Value = 30,
                                    Children =
                                    [
                                        new Node { Value = 40 },
                                        new Node { Value = 40 },
                                    ]
                                },
                            ]
                        },
                        new Node
                        {
                            Value = 20,
                            Children =
                            [
                                new Node { Value = 30 },
                                new Node
                                {
                                    Value = 30,
                                    Children =
                                    [
                                        new Node
                                        {
                                            Value = 40,
                                            Children =
                                            [
                                                new Node { Value = 50 },
                                                new Node { Value = 50 },
                                            ]
                                        }
                                    ]
                                },
                            ]
                        },
                        new Node
                        {
                            Value = 20,
                            ParentId = 1,
                            Children =[]
                        },
                    ]
                },
            ],
            cancellationToken
        );
        await context.SaveChangesAsync(cancellationToken);

        context.ChangeTracker.Clear();
    }

    public async Task EnsureDbSeeded(CancellationToken cancellationToken)
    {
        await context.Database.EnsureCreatedAsync(cancellationToken);

        if (await context.Nodes.AnyAsync(cancellationToken))
        {
            return;
        }

        await SeedAsync(cancellationToken);
    }

    private async Task<HashSet<int>> GetSubtreeIdsAsync
    (
        IEnumerable<int> rootIds,
        CancellationToken cancellationToken
    )
    {
        var subtreeIds = new HashSet<int>();
        var queue = await context.Nodes
            .Where(n => rootIds.Contains(n.Id))
            .Select(n => n.Id)
            .ToListAsync(cancellationToken);

        while (queue.Count > 0)
        {
            foreach (var id in queue)
            {
                subtreeIds.Add(id);
            }

            var level = queue;

            var next = await context.Nodes
                .Where(n => level.Contains(n.ParentId!.Value))
                .Select(n => n.Id)
                .ToListAsync(cancellationToken);
            queue = next.Where(x => !subtreeIds.Contains(x)).ToList();
        }
        return subtreeIds;
    }
}
