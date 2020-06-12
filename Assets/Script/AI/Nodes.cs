using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NodeStates
{
    SUCCESS,
    RUNNING,
    FAILURE
}

[System.Serializable]
public abstract class Node
{

    /* Delegate that returns the state of the node.*/
    public delegate NodeStates NodeReturn();

    /* The current state of the node */
    protected NodeStates m_nodeState;

    public NodeStates nodeState
    {
        get { return m_nodeState; }
    }

    /* The constructor for the node */
    public Node() { }

    /* Implementing classes use this method to evaluate the desired set of conditions */
    public abstract NodeStates Evaluate();
}

public class Parallel : Node
{
    protected List<Node> m_nodes = new List<Node>();


    /** The constructor requires a lsit of child nodes to be  
     * passed in*/
    public Parallel(List<Node> nodes)
    {
        m_nodes = nodes;
    }

    public override NodeStates Evaluate()
    {
        foreach (Node node in m_nodes)
        {
            if (node == null)
            {
                continue;
            }
            switch (node.Evaluate())
            {
                case NodeStates.FAILURE:
                    continue;
                case NodeStates.SUCCESS:
                    m_nodeState = NodeStates.SUCCESS;
                    return m_nodeState;
                case NodeStates.RUNNING:
                    m_nodeState = NodeStates.RUNNING;
                    return m_nodeState;
                default:
                    continue;
            }
        }
        m_nodeState = NodeStates.FAILURE;
        return m_nodeState;
    }
}

public class Selector : Node
{
    /** The child nodes for this selector */
    protected List<Node> m_nodes = new List<Node>();


    /** The constructor requires a lsit of child nodes to be  
     * passed in*/
    public Selector(List<Node> nodes)
    {
        m_nodes = nodes;
    }

    /* If any of the children reports a success, the selector will 
     * immediately report a success upwards. If all children fail, 
     * it will report a failure instead.*/
    public override NodeStates Evaluate()
    {
        foreach (Node node in m_nodes)
        {
            if (node == null)
            {
                continue;
            }
            switch (node.Evaluate())
            {
                case NodeStates.FAILURE:
                    continue;
                case NodeStates.SUCCESS:
                    m_nodeState = NodeStates.SUCCESS;
                    return m_nodeState;
                case NodeStates.RUNNING:
                    m_nodeState = NodeStates.RUNNING;
                    return m_nodeState;
                default:
                    continue;
            }
        }
        m_nodeState = NodeStates.FAILURE;
        return m_nodeState;
    }
}

public class Sequence : Node
{
    /** Children nodes that belong to this sequence */
    private List<Node> m_nodes = new List<Node>();

    /** Must provide an initial set of children nodes to work */
    public Sequence(List<Node> nodes)
    {
        m_nodes = nodes;
    }

    /* If any child node returns a failure, the entire node fails. Whence all  
     * nodes return a success, the node reports a success. */
    public override NodeStates Evaluate()
    {
        bool anyChildRunning = false;

        foreach (Node node in m_nodes)
        {
            if (node == null)
            {
                continue;
            }
            switch (node.Evaluate())
            {
                case NodeStates.FAILURE:
                    m_nodeState = NodeStates.FAILURE;
                    return m_nodeState;
                case NodeStates.SUCCESS:
                    continue;
                case NodeStates.RUNNING:
                    anyChildRunning = true;
                    m_nodeState = NodeStates.RUNNING;
                    return m_nodeState;
                default:
                    m_nodeState = NodeStates.SUCCESS;
                    return m_nodeState;
            }
        }
        m_nodeState = anyChildRunning ? NodeStates.RUNNING : NodeStates.SUCCESS;
        return m_nodeState;
    }
}

public class ActionNode : Node
{
    /* Method signature for the action. */
    public delegate NodeStates ActionNodeDelegate();

    /* The delegate that is called to evaluate this node */
    private ActionNodeDelegate m_action;
    
    public ActionNode(ActionNodeDelegate action)
    {
        m_action = action;
    }

    /* Evaluates the node using the passed in delegate and  
     * reports the resulting state as appropriate */
    public override NodeStates Evaluate()
    {
        switch (m_action())
        {
            case NodeStates.SUCCESS:
                m_nodeState = NodeStates.SUCCESS;
                return m_nodeState;
            case NodeStates.FAILURE:
                m_nodeState = NodeStates.FAILURE;
                return m_nodeState;
            case NodeStates.RUNNING:
                m_nodeState = NodeStates.RUNNING;
                return m_nodeState;
            default:
                m_nodeState = NodeStates.FAILURE;
                return m_nodeState;
        }
    }
}
public class Inverter : Node
{
    /* Child node to evaluate */
    private Node m_node;

    public Node node
    {
        get { return m_node; }
    }

    /* The constructor requires the child node that this inverter decorator 
     * wraps*/
    public Inverter(Node node)
    {
        m_node = node;
    }
    /* Reports a success if the child fails and 
     * a failure if the child succeeds. Running will report 
     * as running */
    public override NodeStates Evaluate()
    {
        switch (m_node.Evaluate())
        {
            case NodeStates.FAILURE:
                m_nodeState = NodeStates.SUCCESS;
                return m_nodeState;
            case NodeStates.SUCCESS:
                m_nodeState = NodeStates.FAILURE;
                return m_nodeState;
            case NodeStates.RUNNING:
                m_nodeState = NodeStates.RUNNING;
                return m_nodeState;
        }
        m_nodeState = NodeStates.SUCCESS;
        return m_nodeState;
    }
}

public class VisionNode : Node
{
    private Node m_node;
    BaseAI basicEnemy;
    Vector3 facingDir;
    public Node node
    {
        get { return m_node; }
    }

    /* The constructor requires the child node that this inverter decorator 
     * wraps*/
    public VisionNode(Node node, BaseAI basicEnemy)
    {
        m_node = node;
        this.basicEnemy = basicEnemy;
    }
    public override NodeStates Evaluate()
    {
        Transform hit;
        if (basicEnemy.VisionCheck(1<<8, 1<<1, out hit))
        {
            basicEnemy.visionSubject = hit;
            return m_node.Evaluate();
        }
        basicEnemy.visionSubject = null;
        return NodeStates.FAILURE;
    }
}