using UnityEngine;
using System.Collections;
using System;

namespace OPEN.PandemicAI
{
    /// <summary>
    /// Processes timeline events asynchronously using Unity coroutines.
    /// </summary>
    public class EventProcessor
    {
        private bool isProcessingEvent = false;
        private bool isReady = false;

        /// <summary>
        /// Indicates whether the processor is ready for new events.
        /// </summary>
        public bool IsReady => isReady;

        /// <summary>
        /// Indicates whether an event is currently being processed.
        /// </summary>
        public bool IsProcessingEvent => isProcessingEvent;

        /// <summary>
        /// Optional delay to wait before processing the next event.
        /// </summary>
        private float executeDelay = 0;

        /// <summary>
        /// Reference to the Timeline.
        /// </summary>
        private Timeline timeline;

        /// <summary>
        /// Reference to the running event processing coroutine.
        /// </summary>
        private Coroutine processEventsCoroutine;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventProcessor"/> class.
        /// </summary>
        /// <param name="timeline">The timeline from which to process events.</param>
        public EventProcessor(Timeline timeline)
        {
            this.timeline = timeline;
        }

        /// <summary>
        /// Starts processing events.
        /// </summary>
        /// <returns>Coroutine enumerator.</returns>
        public IEnumerator ProcessEvents()
        {
            processEventsCoroutine = timeline.StartCoroutine(ProcessEventsCoroutine());
            yield return processEventsCoroutine;
        }

        /// <summary>
        /// Coroutine that continuously processes timeline events.
        /// </summary>
        private IEnumerator ProcessEventsCoroutine()
        {
            while (true)
            {
                // If a delay has been set, wait before processing the next event.
                if (executeDelay > 0)
                {
                    yield return new WaitForSeconds(executeDelay);
                    executeDelay = 0;
                }

                ITimelineEvent timelineEvent = timeline.DequeueEvent();
                if (timelineEvent != null)
                {
                    isReady = false;
                    isProcessingEvent = true;
                    try
                    {
                        timelineEvent.Execute(timeline);
                        timelineEvent.Notify();
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                    isProcessingEvent = false;

                    // Wait for any specified delay after processing the event.
                    float delay = timelineEvent.GetDelay();
                    if (delay > 0)
                    {
                        yield return new WaitForSeconds(delay);
                    }
                }
                else
                {
                    isReady = true;
                }
                yield return new WaitForEndOfFrame();
            }
        }

        /// <summary>
        /// Stops processing events by stopping the running coroutine.
        /// </summary>
        public void StopProcessing()
        {
            if (processEventsCoroutine != null)
            {
                timeline.StopCoroutine(processEventsCoroutine);
            }
        }
    }
}
