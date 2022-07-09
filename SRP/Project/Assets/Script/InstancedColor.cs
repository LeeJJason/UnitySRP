using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstancedColor : MonoBehaviour
{
    [SerializeField]
    Color color;
    private void Awake()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        var propertyBlock = new MaterialPropertyBlock();
        propertyBlock.SetColor("_Color", color);
        GetComponent<MeshRenderer>().SetPropertyBlock(propertyBlock);
    }
}
