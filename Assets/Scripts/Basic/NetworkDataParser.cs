using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NetworkDataParser
{
    public abstract NetworkData ParseFromString(string JSONStr);
}
