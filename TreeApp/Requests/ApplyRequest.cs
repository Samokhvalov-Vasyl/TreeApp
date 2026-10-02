using TreeApp.DataLayer;

namespace TreeApp.Requests;

public record ApplyRequest(List<Node> Updates, List<AddNodeRequest> Adds, List<int> Deletes);

public record AddNodeRequest(int TempId, int ParentId, int Value);
