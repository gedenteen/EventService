using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EventQueue
{
    public Queue<EventData> events;

    public EventQueue()
    {
        events = new Queue<EventData>();
    }
}
