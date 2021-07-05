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

        public Action<string> consoleLog = null;

        public TracerController(LinkAnimator linkAnimator, TimeoutWaiter timeoutWaiter)
        {
            this.tracerAnimator = linkAnimator;
            this.timeoutWaiter = timeoutWaiter;
            this.blocked = false;
        }

        //trace complete action setter
        public TracerController SetTraceCompletedAction(Action traceCompletedAction)
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

        public TracerController SetTracePath(List<NetworkNode> tracePath)
        {
            this.traceQueue = new Queue<NetworkNode>();
            tracePath.ForEach(node => traceQueue.Enqueue(node));
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

            if (blocked)
            {
                if (consoleLog != null)
                    consoleLog("Tracer is blocked!");
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
                        .Start(() => {
                            int tracerDelay = traceQueue.Peek().GetTracerDelay();
                            consoleLog(String.Format("Tracer delayed for {0} secs", tracerDelay));
                            timeoutWaiter.Wait(tracerDelay, nodeTraceAction);
                            if (traceQueue.Count > 1)
                                consoleLog(String.Format("Tracer continued..."));
                        });
                }
                else
                    traceCompletedAction();
            };
            
            tracerAnimator
                .SetLinkColor(Color.red)
                .SetStartPoint(firstNode.GetPosition())
                .SetEndPoint(traceQueue.Peek().GetPosition())
                .SetHackingDuration(traceQueue.Peek().GetHackingDuration() * tracingSpeedModifier)
                .Start(() => {
                    int tracerDelay = traceQueue.Peek().GetTracerDelay();
                    consoleLog(String.Format("Tracer delayed for {0} secs", tracerDelay));
                    timeoutWaiter.Wait(tracerDelay, nodeTraceAction);
                    if (traceQueue.Count > 1)
                        consoleLog(String.Format("Tracer continued..."));
                });
        }
        
    }
}
