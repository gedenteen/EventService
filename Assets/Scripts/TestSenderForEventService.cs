using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks; // UniTask

public class TestSenderForEventService : MonoBehaviour
{
    [SerializeField] private EventService _eventService;
    private const float _delay = 5f;

    private void Start()
    {
        if (_eventService == null)
        {
            Debug.LogError("TestSenderForEventService: i have no link for EventService");
            return;
        }

        SendEvents().Forget(); // run UniTask and forget
    }

    private async UniTask SendEvents()
    {
        _eventService.TrackEvent("gameStart", System.DateTime.Now.ToString());

        while (true)
        {
            _eventService.TrackEvent("testEvent", "testValue");
            await UniTask.WaitForSeconds(_delay);
        }
    }
}
