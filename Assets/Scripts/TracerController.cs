using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace TwoDesperadosTest
{

    public class TracerController
    {
        private LinkAnimator tracerAnimator;
        private TimeoutWaiter timeoutWaiter;
        private Queue<NetworkNode> traceQueue;

        private float tracingSpeedModifier = 1f;
        private bool blocked;

        private Action nodeTraceAction = null;
        private Action traceCompletedAction = null;

        public TracerController(LinkAnimator linkAnimator, TimeoutWaiter timeoutWaiter)
        {
            this.tracerAnimator = linkAnimator;
            this.timeoutWaiter = timeoutWaiter;
            this.blocked = false;
        }

        //trace complete action setter
        public TracerController SetTraceCompletedaction(Action traceCompletedAction)
        {
            this.traceCompletedAction = traceCompletedAction;
            return this;
        }

        public TracerController DecreaseTracerSpeed(int percent)
        {
            float newPercent = 100 - percent;
            tracingSpeedModifier = (float) newPercent / 100f;
            return this;
        }

        public void BlockTracer()
        {
            blocked = true;
            tracerAnimator.Stop();
        }
        
        //API method
        public void TraceHackingSignal(List<NetworkNode> tracePath)
        {

            Debug.Log("Tracing commencing!!!!!");
            tracePath.ForEach(node => Debug.LogFormat("{0}", node.ToString()));

            if (blocked)
            {
                Debug.Log("Tracer blocked!!!!!");
                return;
            }

            traceQueue = new Queue<NetworkNode>();

            tracePath.ForEach(node => traceQueue.Enqueue(node));

            NetworkNode firstNode = traceQueue.Dequeue();

            nodeTraceAction = () =>
            {

                if (blocked)
                    return;

                if (traceQueue.Count > 1)
                {
                    tracerAnimator
                        .SetStartPoint(traceQueue.Dequeue().GetPosition())
                        .SetEndPoint(traceQueue.Peek().GetPosition())
                        .SetHackingDuration(traceQueue.Peek().GetHackingDuration() * tracingSpeedModifier)
                        .Start(() => timeoutWaiter.Wait(traceQueue.Peek().GetTracerDelay(), nodeTraceAction));
                }
                else
                    traceCompletedAction();
            };
            
            tracerAnimator
                .SetLinkColor(Color.red)
                .SetStartPoint(firstNode.GetPosition())
                .SetEndPoint(traceQueue.Peek().GetPosition())
                .SetHackingDuration(traceQueue.Peek().GetHackingDuration() * tracingSpeedModifier)
                .Start(() => timeoutWaiter.Wait(firstNode.GetTracerDelay(), nodeTraceAction));
        }
        
    }
}
