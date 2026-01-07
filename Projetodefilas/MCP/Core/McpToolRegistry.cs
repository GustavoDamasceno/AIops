using System.Collections.Concurrent;

namespace McpServer.Core;

public sealed class McpToolRegistry
{
    private readonly ConcurrentDictionary<string, IMcpTool> _tools = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IMcpTool tool) => _tools[tool.Name] = tool;

    public bool TryResolve(string name, out IMcpTool? tool) => _tools.TryGetValue(name, out tool);

    public IEnumerable<IMcpTool> ListTools() => _tools.Values.ToArray();
}


