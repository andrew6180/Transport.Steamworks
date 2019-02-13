using UnityEngine;

namespace Transport.Steamworks.Helpers
{
    /// <summary>
    /// https://garry.tv/2018/01/16/timesince/
    /// </summary>
    public struct TimeSince
    {
        float _time;
 
        public static implicit operator float(TimeSince ts)
        {
            return Time.time - ts._time;
        }
 
        public static implicit operator TimeSince(float ts)
        {
            return new TimeSince { _time = Time.time - ts };
        }
    }
}