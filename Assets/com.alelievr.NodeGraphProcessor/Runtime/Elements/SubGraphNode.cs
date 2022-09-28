using System.Collections.Generic;
using GraphProcessor;
using UnityEngine;
using UnityEngine.UIElements;
using System;

[System.Serializable, NodeMenuItem("Subgraph")]
public class SubGraphNode : BaseNode
{

    [SerializeField]
    private SubGraph subGraph;

    [Input, CustomBehaviourOnly]
    private object _ingress;

    [Output, CustomBehaviourOnly]
    private object _egress;

    private Dictionary<PortData, object> _passThroughBufferByPort = new();

    public override bool HideNodeInspectorBlock => true;
    public override bool needsInspector => true;

    public SubGraph SubGraph => subGraph;

    private List<PortData> IngressPortData => SubGraph?.IngressPortData ?? new List<PortData>();
    private List<PortData> EgressPortData => SubGraph?.EgressPortData ?? new List<PortData>();

    public override void InitializePorts()
    {
        base.InitializePorts();

        _passThroughBufferByPort?.Clear();
        SubGraph?.AddUpdatePortsListener(OnPortsListUpdated);
    }

    protected override void Process()
    {
        base.Process();

        var processor = new ProcessSubGraphProcessor(SubGraph);
        processor.Run(_passThroughBufferByPort);
    }

    [CustomPortInput(nameof(_ingress), typeof(object))]
    protected void PullIngress(List<SerializableEdge> connectedEdges)
    {
        if (connectedEdges.Count == 0) return;

        PortData portData = IngressPortData.Find(x => x.Equals(connectedEdges[0].inputPort.portData));
        _passThroughBufferByPort[portData] = connectedEdges[0].passThroughBuffer;
    }

    [CustomPortBehavior(nameof(_ingress), cloneResults: true)]
    protected IEnumerable<PortData> CreateIngressPorts(List<SerializableEdge> edges)
    {
        if (IngressPortData == null) yield break;

        foreach (var input in IngressPortData)
        {
            if (String.IsNullOrEmpty(input.identifier))
                input.identifier = input.displayName;

            yield return input;
        }
    }

    [CustomPortInput(nameof(_egress), typeof(object))]
    protected void PushEgress(List<SerializableEdge> connectedEdges)
    {
        if (connectedEdges.Count == 0) return;

        PortData portData = EgressPortData.Find(x => x.Equals(connectedEdges[0].outputPort.portData));
        Dictionary<PortData, object> returnedData = SubGraph.EgressNode.PushEgress();
        foreach (var edge in connectedEdges)
        {
            if (returnedData.ContainsKey(portData))
                edge.passThroughBuffer = returnedData[portData];
        }
    }

    [CustomPortBehavior(nameof(_egress), cloneResults: true)]
    protected IEnumerable<PortData> CreateEgressPorts(List<SerializableEdge> edges)
    {
        if (EgressPortData == null) yield break;

        foreach (var output in EgressPortData)
        {
            if (String.IsNullOrEmpty(output.identifier))
                output.identifier = output.displayName;

            yield return output;
        }
    }

    private void OnPortsListUpdated() => UpdateAllPortsLocal();
}