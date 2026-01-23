using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    /// <summary>
    /// Base class for event loggers. Provides a method for formatting event logs
    /// and an abstract method for broadcasting logs.
    /// </summary>
    public abstract class EventLogger
    {
        /// <summary>
        /// Formats the event log output as a JSON string.
        /// </summary>
        /// <param name="timelineEvent">The timeline event to log.</param>
        /// <returns>A formatted log string.</returns>
        public string FormatEventLog(TimelineEvent timelineEvent)
        {
            //Debug.Log($"LOG: {timelineEvent.type} - {timelineEvent.GetType()}");
            var log = new Dictionary<string, object>
            {
                { "timestamp", (Time.time - GameRoot.StartTimestamp).ToString("F3") },
                { "eventType", timelineEvent.GetType().ToString() }
            };

            if (GameRoot.State.CurrentPlayer != null)
            {
                log["currentPlayer"] = new Dictionary<string, object>
                {
                    { "role", GameRoot.State.CurrentPlayer.Role.ToString() },
                    { "name", GameRoot.State.CurrentPlayer.Name }
                };
            }

            var eventData = timelineEvent.GetLogInfo();
            if (eventData != null)
            {
                foreach (var kvp in eventData)
                {
                    log[kvp.Key] = kvp.Value;
                }
            }

            var jsonSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None
            };
            return JsonConvert.SerializeObject(log, jsonSettings);
        }

        /// <summary>
        /// Broadcasts the log for the provided timeline event.
        /// </summary>
        /// <param name="timelineEvent">The event to broadcast.</param>
        public abstract void BroadcastLogs(string type, TimelineEvent timelineEvent);
    }

    /// <summary>
    /// A file-based logger that writes event logs to a file.
    /// </summary>
    public class FileLogger : EventLogger
    {
        private static readonly string fileName = $"log_{DateTime.Now:ddMMyyyy_HHmmss}.txt";
        private static string filePath = string.Empty;

        public FileLogger()
        {
            if (string.IsNullOrEmpty(filePath))
            {
                // Use Application.persistentDataPath for builds, and Application.dataPath for the editor
                string logsFolderPath = Application.isEditor
                    ? Path.Combine(Application.dataPath, "Logs")
                    : Path.Combine(Application.persistentDataPath, "Logs");

                if (!Directory.Exists(logsFolderPath))
                {
                    Directory.CreateDirectory(logsFolderPath);
                }
                filePath = Path.Combine(logsFolderPath, fileName);
            }
        }

        /// <summary>
        /// Writes the formatted event log to the log file.
        /// </summary>
        /// <param name="timelineEvent">The event to log.</param>
        public override void BroadcastLogs(string type, TimelineEvent timelineEvent)
        {
            string logs = FormatEventLog(timelineEvent);
            try
            {
                using (StreamWriter writer = File.AppendText(filePath))
                {
                    writer.WriteLine(logs);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"LOG: Error writing to log file: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// A network logger for broadcasting event logs over the network.
    /// </summary>
    public class FurhatBroadcasting : EventLogger
    {
        Furhat furhat;

        public FurhatBroadcasting()
        {
            furhat = GameObject.FindFirstObjectByType<Furhat>();
        }


        /// <summary>
        /// Broadcasts the event log over the network.
        /// </summary>
        /// <param name="timelineEvent">The event to log.</param>
        public override void BroadcastLogs(string type, TimelineEvent timelineEvent)
        {
            if(furhat!= null)
                furhat.ProcessEvent(type, FormatEventLog(timelineEvent));
        }
    }
}
