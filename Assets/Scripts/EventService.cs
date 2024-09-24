using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text;
using Cysharp.Threading.Tasks; // UniTask
using System.IO;

public class EventService : MonoBehaviour
{
    [SerializeField] SettingsForEventService _mySettings;

    private EventQueue _eventQueue = new EventQueue(); // Event queue
    private string _fullFileNameForEvents;

    private void Awake()
    {
        if (_mySettings == null)
        {
            Debug.LogError("EventService: i have no link for SettingsForEventService");
            return;
        }

        _fullFileNameForEvents = Path.Combine(Application.persistentDataPath, _mySettings.FileNameForEvents);
        Debug.Log($"Start: fullFileNameForEvents: {_fullFileNameForEvents}");

        LoadEventsFromDisk();

        // Start attempting to send events
        ProcessEventQueue().Forget(); // run UniTask and forget
    }

    // A method for other classes that saves the event
    public void TrackEvent(string type, string data)
    {
        // TODO: make sure that events have already been downloaded from disk by this point
        _eventQueue.events.Enqueue(new EventData(type, data));
    }

    // Method for processing the event queue
    private async UniTask ProcessEventQueue()
    {
        while (true)
        {
            if (_eventQueue.events.Count > 0)
            {
                Debug.Log("Attempting to send events...");

                // Try to send events
                bool success = await SendEventsAsync(_eventQueue);

                // If successful, remove events from the queue
                if (success)
                {
                    Debug.Log("Removing sent events from the queue.");
                    _eventQueue.events.Clear(); // Clear the queue after successful send
                    DeleteEventsFromDisk();
                }
                else
                {
                    Debug.Log("Saving events to persistent storage to prevent loss on app restart");
                    SaveEventsToDisk();
                }
            }

            // Pause before next attempt to process the queue
            await UniTask.WaitForSeconds(_mySettings.CooldownBeforeSend);
        }
    }

    // Method for sending events
    private async UniTask<bool> SendEventsAsync(EventQueue eventQueue)
    {
        string json = JsonConvert.SerializeObject(eventQueue, Formatting.Indented);
        Debug.Log($"SendEventsAsync: json:\n{json}");

        using (UnityWebRequest request = new UnityWebRequest(_mySettings.ServerUrl, "POST"))
        {
            // Indicate that the data is sent in JSON format
            request.SetRequestHeader("Content-Type", "application/json");

            // Add body to request
            byte[] jsonToSend = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = new DownloadHandlerBuffer();

            // Send request and wait for response
            UnityWebRequestAsyncOperation asyncOperation = request.SendWebRequest();

            while (!asyncOperation.isDone)
            {
                await UniTask.Yield();
            }

            // Check the result of the request
            if (request.result == UnityWebRequest.Result.Success && request.responseCode == 200)
            {
                Debug.Log("Events sent successfully!");
                return true;
            }
            else
            {
                Debug.LogWarning("Error sending events: " + request.error);
                return false;
            }
        }
    }

    private void SaveEventsToDisk()
    {
        string json = JsonConvert.SerializeObject(_eventQueue, Formatting.Indented);
        File.WriteAllText(_fullFileNameForEvents, json);
        Debug.Log("Events saved to disk");
    }

    private void LoadEventsFromDisk()
    {
        if (File.Exists(_fullFileNameForEvents))
        {
            string json = File.ReadAllText(_fullFileNameForEvents);
            _eventQueue = JsonConvert.DeserializeObject<EventQueue>(json);
            Debug.Log("Events loaded from disk");
        }
    }

    private void DeleteEventsFromDisk()
    {
        if (File.Exists(_fullFileNameForEvents))
        {
            File.Delete(_fullFileNameForEvents);
            Debug.Log($"File deleted successfully");
        }
    }
}
