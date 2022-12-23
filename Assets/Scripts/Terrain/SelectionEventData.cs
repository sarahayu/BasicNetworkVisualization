using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SelectionEventData
{
    public List<NodeData> nodesSelected;
}
public class DeselectionEventData
{
}

[System.Serializable]
public class SelectionEvent : UnityEvent<SelectionEventData>
{

}


[System.Serializable]
public class DeselectionEvent : UnityEvent<DeselectionEventData>
{

}
