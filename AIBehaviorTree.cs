using System.Collections.Generic;
using UnityEngine;

namespace NexusPrime.AI.BehaviorTree
{
    public class AIBehaviorTree
    {
        private AINode root;
        private Dictionary<string, object> blackboard;
        private List<AINode> activeNodes;
        
        public AIBehaviorTree()
        {
            blackboard = new Dictionary<string, object>();
            activeNodes = new List<AINode>();
            InitializeBlackboard();
        }
        
        void InitializeBlackboard()
        {
            blackboard["LastUpdateTime"] = 0f;
            blackboard["TreeState"] = "Running";
            blackboard["CurrentTask"] = "";
            blackboard["DebugMode"] = false;
        }
        
        public void SetRoot(AINode node)
        {
            root = node;
            root.SetBlackboard(blackboard);
        }
        
        public void Update()
        {
            if (root == null)
            {
                Debug.LogError("Behavior Tree has no root node!");
                return;
            }
            
            // Update blackboard
            blackboard["LastUpdateTime"] = Time.time;
            
            // Execute tree
            NodeStatus status = root.Execute();
            
            // Update tree state
            blackboard["TreeState"] = status.ToString();
            
            // Clean up completed nodes
            UpdateActiveNodes();
            
            // Debug output if enabled
            if ((bool)blackboard["DebugMode"])
            {
                DebugTreeStatus();
            }
        }
        
        void UpdateActiveNodes()
        {
            for (int i = activeNodes.Count - 1; i >= 0; i--)
            {
                if (activeNodes[i].GetStatus() != NodeStatus.Running)
                {
                    activeNodes.RemoveAt(i);
                }
            }
        }
        
        public void Reset()
        {
            if (root != null)
            {
                root.Reset();
            }
            activeNodes.Clear();
            blackboard["TreeState"] = "Reset";
        }
        
        public void SetBlackboardValue(string key, object value)
        {
            if (blackboard.ContainsKey(key))
            {
                blackboard[key] = value;
            }
            else
            {
                blackboard.Add(key, value);
            }
        }
        
        public object GetBlackboardValue(string key)
        {
            if (blackboard.ContainsKey(key))
            {
                return blackboard[key];
            }
            return null;
        }
        
        public bool HasBlackboardValue(string key)
        {
            return blackboard.ContainsKey(key);
        }
        
        public void RegisterActiveNode(AINode node)
        {
            if (!activeNodes.Contains(node))
            {
                activeNodes.Add(node);
            }
        }
        
        public void UnregisterActiveNode(AINode node)
        {
            if (activeNodes.Contains(node))
            {
                activeNodes.Remove(node);
            }
        }
        
        public string GetTreeStatus()
        {
            if (root == null) return "No Root";
            return root.GetStatus().ToString();
        }
        
        public string GetTreeStructure()
        {
            if (root == null) return "Empty Tree";
            return GetNodeStructure(root, 0);
        }
        
        string GetNodeStructure(AINode node, int depth)
        {
            string indent = new string(' ', depth * 2);
            string structure = $"{indent}{node.GetType().Name}: {node.name} ({node.GetStatus()})\n";
            
            if (node is AIContainerNode container)
            {
                foreach (AINode child in container.GetChildren())
                {
                    structure += GetNodeStructure(child, depth + 1);
                }
            }
            
            return structure;
        }
        
        void DebugTreeStatus()
        {
            string debugInfo = "=== Behavior Tree Status ===\n";
            debugInfo += $"State: {blackboard["TreeState"]}\n";
            debugInfo += $"Current Task: {blackboard["CurrentTask"]}\n";
            debugInfo += $"Active Nodes: {activeNodes.Count}\n";
            
            foreach (AINode node in activeNodes)
            {
                debugInfo += $"  - {node.name} ({node.GetStatus()})\n";
            }
            
            Debug.Log(debugInfo);
        }
        
        public void EnableDebugMode(bool enable)
        {
            blackboard["DebugMode"] = enable;
        }
        
        public void ForceNodeStatus(string nodeName, NodeStatus status)
        {
            // Find and force a node's status (for debugging)
            ForceNodeStatusRecursive(root, nodeName, status);
        }
        
        void ForceNodeStatusRecursive(AINode node, string nodeName, NodeStatus status)
        {
            if (node.name == nodeName)
            {
                node.ForceStatus(status);
                return;
            }
            
            if (node is AIContainerNode container)
            {
                foreach (AINode child in container.GetChildren())
                {
                    ForceNodeStatusRecursive(child, nodeName, status);
                }
            }
        }
    }
    
    public abstract class AINode
    {
        public string name;
        protected NodeStatus status;
        protected Dictionary<string, object> blackboard;
        
        public AINode(string nodeName)
        {
            name = nodeName;
            status = NodeStatus.Ready;
        }
        
        public void SetBlackboard(Dictionary<string, object> bb)
        {
            blackboard = bb;
        }
        
        public abstract NodeStatus Execute();
        
        public virtual void Reset()
        {
            status = NodeStatus.Ready;
        }
        
        public NodeStatus GetStatus()
        {
            return status;
        }
        
        public void ForceStatus(NodeStatus newStatus)
        {
            status = newStatus;
        }
        
        protected void SetBlackboardValue(string key, object value)
        {
            if (blackboard != null)
            {
                if (blackboard.ContainsKey(key))
                {
                    blackboard[key] = value;
                }
                else
                {
                    blackboard.Add(key, value);
                }
            }
        }
        
        protected object GetBlackboardValue(string key)
        {
            if (blackboard != null && blackboard.ContainsKey(key))
            {
                return blackboard[key];
            }
            return null;
        }
        
        protected bool HasBlackboardValue(string key)
        {
            return blackboard != null && blackboard.ContainsKey(key);
        }
    }
    
    public class AIContainerNode : AINode
    {
        protected List<AINode> children;
        
        public AIContainerNode(string name) : base(name)
        {
            children = new List<AINode>();
        }
        
        public void AddChild(AINode child)
        {
            children.Add(child);
            if (blackboard != null)
            {
                child.SetBlackboard(blackboard);
            }
        }
        
        public void RemoveChild(AINode child)
        {
            children.Remove(child);
        }
        
        public List<AINode> GetChildren()
        {
            return new List<AINode>(children);
        }
        
        public override void Reset()
        {
            base.Reset();
            foreach (AINode child in children)
            {
                child.Reset();
            }
        }
        
        public override NodeStatus Execute()
        {
            return NodeStatus.Ready; // Override in derived classes
        }
    }
    
    public class AISequenceNode : AIContainerNode
    {
        public AISequenceNode(string name) : base(name) { }
        
        public override NodeStatus Execute()
        {
            status = NodeStatus.Running;
            
            foreach (AINode child in children)
            {
                NodeStatus childStatus = child.Execute();
                
                if (childStatus == NodeStatus.Failure)
                {
                    status = NodeStatus.Failure;
                    return status;
                }
                else if (childStatus == NodeStatus.Running)
                {
                    return NodeStatus.Running;
                }
            }
            
            status = NodeStatus.Success;
            return status;
        }
    }
    
    public class AISelectorNode : AIContainerNode
    {
        public AISelectorNode(string name) : base(name) { }
        
        public override NodeStatus Execute()
        {
            status = NodeStatus.Running;
            
            foreach (AINode child in children)
            {
                NodeStatus childStatus = child.Execute();
                
                if (childStatus == NodeStatus.Success)
                {
                    status = NodeStatus.Success;
                    return status;
                }
                else if (childStatus == NodeStatus.Running)
                {
                    return NodeStatus.Running;
                }
            }
            
            status = NodeStatus.Failure;
            return status;
        }
    }
    
    public class AIParallelNode : AIContainerNode
    {
        private int requiredSuccesses;
        private int requiredFailures;
        
        public AIParallelNode(string name, int successes = 1, int failures = int.MaxValue) : base(name)
        {
            requiredSuccesses = successes;
            requiredFailures = failures;
        }
        
        public override NodeStatus Execute()
        {
            status = NodeStatus.Running;
            int successCount = 0;
            int failureCount = 0;
            
            foreach (AINode child in children)
            {
                NodeStatus childStatus = child.GetStatus();
                
                if (childStatus == NodeStatus.Ready || childStatus == NodeStatus.Running)
                {
                    childStatus = child.Execute();
                }
                
                if (childStatus == NodeStatus.Success)
                {
                    successCount++;
                }
                else if (childStatus == NodeStatus.Failure)
                {
                    failureCount++;
                }
                
                if (successCount >= requiredSuccesses)
                {
                    status = NodeStatus.Success;
                    return status;
                }
                
                if (failureCount >= requiredFailures)
                {
                    status = NodeStatus.Failure;
                    return status;
                }
            }
            
            return NodeStatus.Running;
        }
    }
    
    public class AIConditionNode : AINode
    {
        private System.Func<bool> condition;
        
        public AIConditionNode(string name, System.Func<bool> conditionFunc) : base(name)
        {
            condition = conditionFunc;
        }
        
        public override NodeStatus Execute()
        {
            try
            {
                bool result = condition();
                status = result ? NodeStatus.Success : NodeStatus.Failure;
                return status;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Condition node '{name}' failed: {e.Message}");
                status = NodeStatus.Failure;
                return status;
            }
        }
    }
    
    public class AIActionNode : AINode
    {
        private System.Func<NodeStatus> action;
        private float timeout = 0f;
        private float startTime;
        
        public AIActionNode(string name, System.Func<NodeStatus> actionFunc, float actionTimeout = 0f) : base(name)
        {
            action = actionFunc;
            timeout = actionTimeout;
        }
        
        public override void Reset()
        {
            base.Reset();
            startTime = 0f;
        }
        
        public override NodeStatus Execute()
        {
            if (status == NodeStatus.Ready)
            {
                startTime = Time.time;
                SetBlackboardValue("CurrentTask", name);
            }
            
            status = NodeStatus.Running;
            
            // Check timeout
            if (timeout > 0 && Time.time - startTime > timeout)
            {
                Debug.LogWarning($"Action node '{name}' timed out after {timeout} seconds");
                status = NodeStatus.Failure;
                return status;
            }
            
            try
            {
                NodeStatus result = action();
                status = result;
                
                if (result != NodeStatus.Running)
                {
                    SetBlackboardValue("CurrentTask", "");
                }
                
                return status;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Action node '{name}' failed: {e.Message}");
                status = NodeStatus.Failure;
                SetBlackboardValue("CurrentTask", "");
                return status;
            }
        }
    }
    
    public class AIRepeatNode : AIContainerNode
    {
        private int repeatCount;
        private int currentCount;
        private bool infinite;
        
        public AIRepeatNode(string name, int count = 0) : base(name)
        {
            repeatCount = count;
            currentCount = 0;
            infinite = count <= 0;
        }
        
        public override void Reset()
        {
            base.Reset();
            currentCount = 0;
        }
        
        public override NodeStatus Execute()
        {
            if (children.Count == 0)
            {
                status = NodeStatus.Success;
                return status;
            }
            
            AINode child = children[0];
            NodeStatus childStatus = child.Execute();
            
            if (childStatus == NodeStatus.Running)
            {
                status = NodeStatus.Running;
                return status;
            }
            
            // Child completed, check if we should repeat
            if (childStatus == NodeStatus.Success || childStatus == NodeStatus.Failure)
            {
                child.Reset();
                currentCount++;
                
                if (!infinite && currentCount >= repeatCount)
                {
                    status = NodeStatus.Success;
                    return status;
                }
                
                // Continue repeating
                status = NodeStatus.Running;
                return status;
            }
            
            status = childStatus;
            return status;
        }
    }
    
    public class AIInverterNode : AIContainerNode
    {
        public AIInverterNode(string name) : base(name) { }
        
        public override NodeStatus Execute()
        {
            if (children.Count == 0)
            {
                status = NodeStatus.Success;
                return status;
            }
            
            AINode child = children[0];
            NodeStatus childStatus = child.Execute();
            
            switch (childStatus)
            {
                case NodeStatus.Success:
                    status = NodeStatus.Failure;
                    break;
                case NodeStatus.Failure:
                    status = NodeStatus.Success;
                    break;
                default:
                    status = childStatus;
                    break;
            }
            
            return status;
        }
    }
    
    public class AIDecoratorNode : AINode
    {
        private AINode child;
        private System.Func<AINode, NodeStatus> decoratorFunc;
        
        public AIDecoratorNode(string name, System.Func<AINode, NodeStatus> func) : base(name)
        {
            decoratorFunc = func;
        }
        
        public void SetChild(AINode node)
        {
            child = node;
            if (blackboard != null)
            {
                child.SetBlackboard(blackboard);
            }
        }
        
        public override void Reset()
        {
            base.Reset();
            if (child != null)
            {
                child.Reset();
            }
        }
        
        public override NodeStatus Execute()
        {
            if (child == null)
            {
                status = NodeStatus.Success;
                return status;
            }
            
            try
            {
                status = decoratorFunc(child);
                return status;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Decorator node '{name}' failed: {e.Message}");
                status = NodeStatus.Failure;
                return status;
            }
        }
    }
    
    public enum NodeStatus
    {
        Ready,
        Running,
        Success,
        Failure
    }
}