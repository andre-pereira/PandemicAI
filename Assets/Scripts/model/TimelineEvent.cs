using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace OPEN.PandemicAI
{
    public interface ITimelineEvent
    {
        void Execute(Timeline timeline);
        void Notify();
        float GetDelay();
    }

    [Serializable]
    public abstract class TimelineEvent : ITimelineEvent
    {
        private static List<EventLogger> _eventLoggers = new List<EventLogger> { new FileLogger(), new FurhatBroadcasting()};

        public virtual void Do(Timeline timeline) { }
        public virtual float Act() { return 0; }
        public virtual Dictionary<string, object> GetLogInfo() {return null;}
        public virtual void Notify()
        {
            foreach (EventLogger logger in _eventLoggers)
            {
                logger.BroadcastLogs(GetType().Name, this);
            }   
        }

        public void Subscribe(EventLogger logger)
        {
            _eventLoggers.Add(logger);
        }

        public void Unsubscribe(EventLogger logger)
        {
            _eventLoggers.Remove(logger);
        }

        public void Execute(Timeline timeline)
        {
            Do(timeline);
        }

        public float GetDelay()
        {
            if (GameRoot.Config.SimulationMode) return 0;

            return Act();
        }
    }

    [Serializable]
    public abstract class EngineEvent : TimelineEvent
    {

    }

    [Serializable]
    public abstract class PlayerEvent : TimelineEvent
    {
        [JsonIgnore]
        public Player _player;

        [JsonIgnore]
        public PlayerPanel _panel;

        public PlayerEvent() 
        {
            _player = GameRoot.State.CurrentPlayer;
            _panel = _player.Panel;
        }

        public PlayerEvent(Player player)
        {
            _player = player;
            _panel = player.Panel;

        }
    }

    public abstract class GuiEvent : PlayerEvent
    {
        public GuiEvent(Player player) : base(player)
        {
            _player = player;
            _panel = player.Panel;
        }
    }

    [Serializable]
    public class EDelay : EngineEvent
    {
        private float myDelay;

        public EDelay(float delay)
        {
            myDelay = delay;
        }

        public override void Do(Timeline timeline) {}

        public override float Act() {return myDelay;}
    }
}
