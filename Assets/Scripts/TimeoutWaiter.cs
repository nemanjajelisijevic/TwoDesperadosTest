using System.Collections;
using System;
using UnityEngine;

namespace TwoDesperadosTest
{
    public class TimeoutWaiter
    {
        private MonoBehaviour coroutineHolder;

        public TimeoutWaiter(MonoBehaviour coroutineHolder)
        {
            this.coroutineHolder = coroutineHolder;
        }

        public void Wait(float interval)
        {
            Wait(interval, null);
        }

        public void Wait(float interval, Action action)
        {
            coroutineHolder.StartCoroutine(WaitCoroutine(interval, action));
        }

        private IEnumerator WaitCoroutine(float interval, Action action)
        {
            yield return new WaitForSeconds(interval);

            if (action != null)
                action();
        }

    }

}