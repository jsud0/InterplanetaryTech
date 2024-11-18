using System;
using SFS.Parts.Modules;
using UnityEngine;

namespace InterplanetaryModule
{
    public class RepeatMoveModule : MonoBehaviour
    {
        public bool activated;
        public float time;
        public float duration;
        public bool unscaledTime;
        public EngineModule engineModule;
        public MoveModule moveModule;

        private void Start()
        {
            moveModule.Activate();
        }

        private void Update()
        {
            if (!activated)
                return;
            float num = this.unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (num == 0f)
                return;
            float maxDelta = (duration > 0f) ? (num / duration) : 10000f;
            bool enabled = engineModule == null ? true : engineModule.engineOn.Value && engineModule.throttle_Out.Value > 0f;
            bool finished = moveModule.time.Value >= 1f;
            if (enabled)
            {
                time = Mathf.MoveTowards(time, 1, maxDelta);
                if (time >= 1)
                    time = 0;
                moveModule.enabled = true;
                moveModule.time.Value = time;
            }
            else
            {
                if (!finished)
                    time = Mathf.MoveTowards(time, 1, maxDelta);
                else
                    moveModule.enabled = false;
            }
        }
    }
}