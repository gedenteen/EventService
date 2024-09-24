using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;

public class EventService : MonoBehaviour
{
    private const string serverUrl = "https://my-server-url.com/api/events"; // URL сервера

    private void Start()
    {
        EventQueue eventQueue = new EventQueue();
        eventQueue.events.Enqueue(new EventData("levelStart", "level:1"));
        eventQueue.events.Enqueue(new EventData("earnMoney", "moneyAmount:810"));
        eventQueue.events.Enqueue(new EventData("levelStart", "level:2"));
        SendEventsAsync(eventQueue);
    }

    public async Task SendEventsAsync(EventQueue eventQueue)
    {
        string json = JsonConvert.SerializeObject(eventQueue, Formatting.Indented);
        Debug.Log($"SendEventsAsync: json:\n{json}");

        // Create POST-request
        using (UnityWebRequest request = new UnityWebRequest(serverUrl, "POST"))
        {
            // Indicate that the data is sent in JSON format
            request.SetRequestHeader("Content-Type", "application/json");

            // Add body to request
            byte[] jsonToSend = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = new DownloadHandlerBuffer();

            // Send request and wait for a response
            UnityWebRequestAsyncOperation asyncOperation = request.SendWebRequest();

            while (!asyncOperation.isDone)
            {
                await Task.Yield();
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("The events have been sent successfully!");
            }
            else
            {
                Debug.LogError($"Error sending events: {request.error}");
            }
        }
    }
}
