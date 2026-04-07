using System;
using UnityEngine;
namespace BehaviorTree
{
    public class BTRunner<TContext>
    {
        public TContext Context { get; }
        public INode<TContext> Tree { get; }

        public BTRunner(TContext context, INode<TContext> tree)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Tree = tree ?? throw new ArgumentNullException(nameof(tree));
        }

        public void Tick(float duration)
        {
            Tree.Tick(Context, duration);
        }

        public void Abort()
        {
            Tree.Abort(Context);
        }

        public string PrintTree(IReadOnlyNode node, int depth = 0)
        {
            string indent = new string(' ', depth * 4);
            string treeLog = $"{indent}- {node.Name} [{node.Status}]\n";

            if (node.SubNodes == null)
                return treeLog;
                
            foreach (var c in node.SubNodes)
                treeLog += PrintTree(c, depth + 1);

            return treeLog;
        }
    }
}