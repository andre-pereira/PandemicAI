using OPEN.PandemicAI;
using TCPFurhatComm;
using TMPro;
using UnityEngine;
using static UnityEngine.Mathf;          // for Clamp, DeltaAngle …

namespace OPEN.PandemicAI
{
    public static class UserGazeClassifier
    {
        public static UserGazingAt Evaluate(Vector3Simple userPos, Vector3Simple headEulerDeg, GazeSettings gazeSettings)
        {
            float yaw = (float)headEulerDeg.y;
            float pitch = (float)headEulerDeg.x; 

            foreach (var candidate in s_statesToLookFor)
            {
                var zone = gazeSettings.GetZone(candidate);

                if (InAngleRange(yaw, zone.yawMin, zone.yawMax) &&
                    InAngleRange(pitch, zone.pitchMin, zone.pitchMax))
                {
                    return candidate;
                }
            }
            if(yaw < 180)
            {
                return UserGazingAt.ElsewhereLeft;
            }
            else return UserGazingAt.ElseWhereRight;
        }

        public static (bool, UserGazingAt) UpdateState(Vector3Simple userPos, Vector3Simple headEulerDeg, GazeSettings gazeSettings, UserGazingAt currentState )
        {
            var next = Evaluate(userPos, headEulerDeg, gazeSettings);
            if (next != currentState)
            {
                currentState = next;
                return (true, next);
            }
            return (false, currentState);
        }

        static readonly UserGazingAt[] s_statesToLookFor = {
            UserGazingAt.Robot,UserGazingAt.Board};


        static bool InAngleRange(float angle, float min, float max)
        {
            return angle > min && angle < max;
        }
    }
}