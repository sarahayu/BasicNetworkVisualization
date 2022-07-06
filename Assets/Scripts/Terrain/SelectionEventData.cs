using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SelectionEventData
{
    public List<TerrainNodeData> groupsSelected;
}

[System.Serializable]
public class SelectionEvent : UnityEvent<SelectionEventData>
{

}
