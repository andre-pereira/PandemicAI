using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using System.IO;

namespace OPEN.PandemicAI
{
	public class DialogActs 
	{
        private System.Random randomGenerator;
        public Dictionary<string, EventDialogMapping> dialogMap;
        
        public void LoadMappings(string furhatPath)
        {
            randomGenerator = new System.Random();
            dialogMap = new Dictionary<string, EventDialogMapping>();
            
            string jsonFromFile = File.ReadAllText(furhatPath);
            var intermediateMappings = JsonConvert.DeserializeObject<List<EventDialogMappingJson>>(jsonFromFile);
            
            foreach (var intermediateMap in intermediateMappings)
            {
                var finalMap = new EventDialogMapping
                {
                    eventType = intermediateMap.eventType,
                    relevance = intermediateMap.relevance,
                    aiTurnBehaviors = intermediateMap.aiTurnBehaviors?.Select(s => new Behavior(s)).ToList(),
                    userTurnBehaviors = intermediateMap.userTurnBehaviors?.Select(s => new Behavior(s)).ToList(),
                    engineTurnBehaviors = intermediateMap.engineTurnBehaviors?.Select(s => new Behavior(s)).ToList(),
                    conditions = intermediateMap.conditions?.Select(c => new Condition
                    {
                        value = c.value,
                        aiTurnBehaviors = c.aiTurnBehaviors?.Select(s => new Behavior(s)).ToList(),
                        userTurnBehaviors = c.userTurnBehaviors?.Select(s => new Behavior(s)).ToList(),
                        engineTurnBehaviors = c.engineTurnBehaviors?.Select(s => new Behavior(s)).ToList()
                    }).ToList()
                };

                // init per-list counters
                InitCounters(finalMap);
                if (finalMap.conditions != null)
                    foreach (var c in finalMap.conditions)
                        InitCounters(c);

                dialogMap.Add(intermediateMap.eventType, finalMap);
            }
        }

        public float GetEventRelevance(string eventType)
        {
            if (dialogMap == null || !dialogMap.ContainsKey(eventType))
                return 0f;

            return dialogMap[eventType].relevance;
        }

        private string GetNextBehavior(IDialogBehaviorSource src, string kind)
        {
            List<Behavior> list = kind switch
            {
                "ai" => src.aiTurnBehaviors,
                "user" => src.userTurnBehaviors,
                "engine" => src.engineTurnBehaviors,
                _ => null
            };
            if (list == null || list.Count == 0) return null;

            // Read current index for this list
            int idx = kind == "ai" ? src.aiIndex
                    : kind == "user" ? src.userIndex
                    : src.engineIndex;

            // Safety: re-init if out of bounds
            if (idx < 0 || idx >= list.Count)
                idx = Mathf.Clamp(Furhat.Instance.DialogActStartingUtterance, 0, list.Count - 1);

            // Select
            Behavior selected = list[idx];

            // Advance + wrap
            int next = (idx + 1) % list.Count;

            // Write back to the correct counter
            if (kind == "ai") src.aiIndex = next;
            else if (kind == "user") src.userIndex = next;
            else src.engineIndex = next;

            selected.nTimesSelected++;
            return selected.text;
        }




        private string GetLeastUsedBehavior(List<Behavior> behaviors)
        {
            if (behaviors == null || behaviors.Count == 0)
                return null;

            int minNumTimesSelected = behaviors.Min(x => x.nTimesSelected);
            List<Behavior> lessUsedBehaviors = behaviors.Where(x => x.nTimesSelected == minNumTimesSelected).ToList();

            if (lessUsedBehaviors.Count == 0)
                return null; // Should not happen if behaviors was not empty, but a safeguard

            int randomIndex = randomGenerator.Next(0, lessUsedBehaviors.Count);
            Behavior selectedBehavior = lessUsedBehaviors[randomIndex];

            selectedBehavior.nTimesSelected++;
            return selectedBehavior.text;
        }

        private string GetDialogFromTurn(bool isAi, IDialogBehaviorSource entry)
        {
            if (isAi)
            {
                string dialog = GetNextBehavior(entry, "ai");
                if (dialog != null) return dialog;

                dialog = GetNextBehavior(entry, "user");
                if (dialog != null) return dialog;

                return GetNextBehavior(entry, "engine");
            }
            else
            {
                string dialog = GetNextBehavior(entry, "user");
                if (dialog != null) return dialog;

                dialog = GetNextBehavior(entry, "engine");
                if (dialog != null) return dialog;

                return GetNextBehavior(entry, "ai");
            }
        }


        public string GetDialog(string eventType, bool isAi, string condition)
        {
            if (dialogMap == null || !dialogMap.ContainsKey(eventType))
                return null;
            var entry = dialogMap[eventType];
           

            if (condition == "")
            {
                return GetDialogFromTurn(isAi, entry);
            }
            else
            {
                foreach (var con in entry.conditions)
                {
                    if (con.value == condition)
                    {
                        return GetDialogFromTurn(isAi, con);
                    }
                }
            }
            return null;
        }
        private void InitCounters(IDialogBehaviorSource src)
        {
            int Start(List<Behavior> list)
                => (list == null || list.Count == 0)
                   ? 0
                   : Mathf.Clamp(Furhat.Instance.DialogActStartingUtterance, 0, list.Count - 1);

            src.aiIndex = Start(src.aiTurnBehaviors);
            src.userIndex = Start(src.userTurnBehaviors);
            src.engineIndex = Start(src.engineTurnBehaviors);
        }

    }

    [System.Serializable]
	public class ConditionJson
	{
		public string value;
		public List<string> aiTurnBehaviors;
        public List<string> userTurnBehaviors;
        public List<string> engineTurnBehaviors;
    }

	[System.Serializable]
	public class EventDialogMappingJson
	{
		public string eventType;
		public float relevance;
		public List<string> aiTurnBehaviors; 
		public List<string> userTurnBehaviors;
        public List<string> engineTurnBehaviors;
        public List<ConditionJson> conditions;
	}

    public class Condition : IDialogBehaviorSource // Implement the interface
    {
        public string value;
        public List<Behavior> aiTurnBehaviors { get; set; } // Add 'set' for interface implementation
        public List<Behavior> userTurnBehaviors { get; set; } // Add 'set'
        public List<Behavior> engineTurnBehaviors { get; set; } // Add 'set'
        public int aiIndex { get; set; }
        public int userIndex { get; set; }
        public int engineIndex { get; set; }
    }

    public class EventDialogMapping : IDialogBehaviorSource // Implement the interface
    {
        public string eventType;
        public float relevance;
        public List<Behavior> aiTurnBehaviors { get; set; } // Add 'set' for interface implementation
        public List<Behavior> userTurnBehaviors { get; set; } // Add 'set'
        public List<Behavior> engineTurnBehaviors { get; set; } // Add 'set'
        public List<Condition> conditions; // Optional
        public int aiIndex { get; set; }
        public int userIndex { get; set; }
        public int engineIndex { get; set; }
    }

    public interface IDialogBehaviorSource
    {
        List<Behavior> aiTurnBehaviors { get; }
        List<Behavior> userTurnBehaviors { get; }
        List<Behavior> engineTurnBehaviors { get; }
        int aiIndex { get; set; }
        int userIndex { get; set; }
        int engineIndex { get; set; }
    }

    public class Behavior
    {
        public string text;
        public int nTimesSelected;

        public Behavior(string text)
        {
            this.text = text;
            nTimesSelected = 0;
        }
    }
    

    }