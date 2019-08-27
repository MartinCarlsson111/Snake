using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public struct Node2<T> where T : struct
{
    T data;
    int index;
    int next;
}

public class Node<T> where T : class
{
    Node<T> next;
    T data;
}
