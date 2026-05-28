namespace BehaviorTree
{
    public static class BTDebugger
    {
        public static string DrawTree(IReadOnlyNode node, int depth = 0)
        {
            string indent = new(' ', depth * 4);
            string treeLog = $"{indent}- {node.Name} [{node.Status}]\n";

            foreach (var c in node.SubNodes)
                treeLog += DrawTree(c, depth + 1);

            return treeLog;
        }
    }
}
