namespace BehaviorTree
{
    public static class BTDebugger
    {
        public static string DrawTree(int serialNum, IReadOnlyNode node, int depth = 0)
        {
            string treeLog;
            string indent = new(' ', depth * 4);
            if (node.SerialNumber != serialNum)
                treeLog = $"{indent}- {node.Name} [None]\n";
            else
                treeLog = $"{indent}- {node.Name} [{node.DisplayInfo}]\n";

            if (node.SubNodes == null)
                return treeLog;
            foreach (var c in node.SubNodes)
                treeLog += DrawTree(serialNum, c, depth + 1);

            return treeLog;
        }
    }
}
