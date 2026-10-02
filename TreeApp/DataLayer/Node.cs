using System.ComponentModel.DataAnnotations.Schema;

namespace TreeApp.DataLayer;

public class Node
{
    public int Id { get; set; }
    public int Value { get; set; }
    public int? ParentId { get; set; } 
    public Node? Parent { get; set; }

    public ICollection<Node> Children { get; set; } = new List<Node>();
}
