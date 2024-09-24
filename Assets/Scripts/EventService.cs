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
    private const string serverUrl = "https://my-server-url.com/api/events"; // URL of server
    private const string fileNameForEvents = "events.json";
    private const float retryDelay = 3f; // Delay between attempts in seconds

    private EventQueue eventQueue = new EventQueue(); // Event queue
    private string fullFileNameForEvents;

    private void Start()
    {
        fullFileNameForEvents = Path.Combine(Application.persistentDataPath, fileNameForEvents);
        Debug.Log($"Start: fullFileNameForEvents: {fullFileNameForEvents}");

        LoadEventsFromDisk();

        // Example of adding events
        eventQueue.events.Enqueue(new EventData("gameStart", System.DateTime.Now.ToString()));

        // Start attempting to send events
        ProcessEventQueue();
    }

    // Method for sending events
    public async UniTask<bool> SendEventsAsync(EventQueue eventQueue)
    {
        string json = JsonConvert.SerializeObject(eventQueue, Formatting.Indented);
        Debug.Log($"SendEventsAsync: json:\n{json}");

        using (UnityWebRequest request = new UnityWebRequest(serverUrl, "POST"))
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

    // Method for processing the event queue
    private async UniTask ProcessEventQueue()
    {
        while (true)
        {
            if (eventQueue.events.Count > 0)
            {
                Debug.Log("Attempting to send events...");

                // Try to send events
                bool success = await SendEventsAsync(eventQueue);

                // If successful, remove events from the queue
                if (success)
                {
                    Debug.Log("Removing sent events from the queue.");
                    eventQueue.events.Clear(); // Clear the queue after successful send
                    DeleteEventsFromDisk();
                }
                else
                {
                    Debug.Log("Saving events to persistent storage to prevent loss on app restart");
                    SaveEventsToDisk();
                }
            }

            // Pause before next attempt to process the queue
            await UniTask.WaitForSeconds(retryDelay);
        }
    }

    private void SaveEventsToDisk()
    {
        string json = JsonConvert.SerializeObject(eventQueue, Formatting.Indented);
        File.WriteAllText(fullFileNameForEvents, json);
        Debug.Log("Events saved to disk");
    }

    private void LoadEventsFromDisk()
    {
        if (File.Exists(fullFileNameForEvents))
        {
            string json = File.ReadAllText(fullFileNameForEvents);
            eventQueue = JsonConvert.DeserializeObject<EventQueue>(json);
            Debug.Log("Events loaded from disk");
        }
    }

    private void DeleteEventsFromDisk()
    {
        if (File.Exists(fullFileNameForEvents))
        {
            File.Delete(fullFileNameForEvents);
            Debug.Log($"File deleted successfully");
        }
    }
}
