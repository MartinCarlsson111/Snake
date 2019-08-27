using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public struct SnakeBody : IComponentData
{
    public int index;
    public Entity entity;

    public float3 target;
    public float size;
}
