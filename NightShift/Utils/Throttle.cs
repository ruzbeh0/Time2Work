using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Time2Work.Utils
{
    public class Throttle
    {
        public float Duration { get; private set; }
        public float Elapsed { get; private set; }
        public Action Action { get; }

        public Throttle(Action action) : this(1, 0, action) { }
        public Throttle(float duration, Action action) : this(duration, 0, action) { }
        public Throttle(float duration, float elapsed, Action action)
        {
            Duration = duration;
            Elapsed = elapsed;
            Action = action;
        }

        public static Throttle BySeconds(float durationSec, Action action) => new Throttle(durationSec, action);
        public static Throttle ByMilliSeconds(float durationMilliSec, Action action) => BySeconds(durationMilliSec / 1000f, action);

        public void Reset()
        {
            Elapsed = 0;
        }

        public void InvokeAction()
        {
            Elapsed = 0;
            Action();
        }

        public void Update(float delta)
        {
            Elapsed += delta;

            if (Elapsed >= Duration)
            {
                InvokeAction();
            }
        }

    }
}
