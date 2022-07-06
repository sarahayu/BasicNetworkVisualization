using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExampleListener : MonoBehaviour
{
    public void OnSelectEvent(SelectionEventData eventData)
    {
        var outStr = "Example: ";

        foreach (var node in eventData.groupsSelected)
            outStr += node.size + ",";

        print(outStr);
    }
}
