using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public struct SnakeHead : IComponentData
{
    public float size;
}

public struct SnakeBodyBuffer : IBufferElementData
{
    public float3 position;
    public quaternion rotation;
}