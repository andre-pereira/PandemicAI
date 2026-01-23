using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace OPEN.PandemicAI
{
    public class Timeline : MonoBehaviour
    {
        private static Timeline instance;
        public static Timeline Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<Timeline>();
                    if (instance == null)
                    {
                        Debug.LogError("No Timeline instance found in the scene.");
                    }
                }
                return instance;
            }
        }


        private Queue<ITimelineEvent> pendingEvents = new Queue<ITimelineEvent>();
        private List<ITimelineEvent> processedEvents = new List<ITimelineEvent>();
        private EventProcessor eventProcessor;
        public bool HasPendingEvent => pendingEvents.Count > 0;
        public IReadOnlyList<ITimelineEvent> ProcessedEvents => processedEvents.AsReadOnly();
        public bool IsProcessorReady => eventProcessor.IsReady;
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            eventProcessor = new EventProcessor(this);
            StartCoroutine(eventProcessor.ProcessEvents());
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public void ResetTimeline()
        {
            pendingEvents.Clear();
            processedEvents.Clear();
        }

        public void AddEvent(ITimelineEvent timelineEvent)
        {
            //Debug.Log("AddEvent: " + timelineEvent.ToString());
            pendingEvents.Enqueue(timelineEvent);

            if (pendingEvents.Count > 1000)
            {
                Debug.LogWarning($"Timeline::AddEvent - Warning: There are {pendingEvents.Count} pending events.");
            }
        }

        public void ClearPendingEvents()
        {
            pendingEvents.Clear();
        }

        public int GetQueueSize() => pendingEvents.Count;

        internal ITimelineEvent DequeueEvent()
        {
            if (pendingEvents.Count > 0)
            {
                ITimelineEvent timelineEvent = pendingEvents.Dequeue();
                processedEvents.Add(timelineEvent);
                return timelineEvent;
            }
            return null;
        }

        internal void AddEvent(object randomAction)
        {
            throw new NotImplementedException();
        }
    }
}
