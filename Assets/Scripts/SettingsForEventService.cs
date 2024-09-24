using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SettingsForEventService", menuName = "New SettingsForEventService")]
public class SettingsForEventService : ScriptableObject
{
    public string ServerUrl = "https://my-server-url.com/api/events"; // URL of server
    public string FileNameForEvents = "events.json"; // Filename for saving events
    public float CooldownBeforeSend = 3f; // Delay before send events to server
}
