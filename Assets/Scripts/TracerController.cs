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
        private NetworkNode currentTracingNode = null;

        private float tracingSpeedModifier = 1f;
        private bool blocked;

        private Action nodeTraceAction = null;
        private Action traceCompletedAction = null;

        public Action<string> consoleLog = null;

        public TracerController(LinkAnimator linkAnimator, TimeoutWaiter timeoutWaiter)
        {
            this.tracerAnimator = linkAnimator;
            this.timeoutWaiter = timeoutWaiter;
            this.traceQueue = new Queue<NetworkNode>();
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
            if (traceQueue.Count > 1) //protection against tracing the last node 
            {
                traceQueue.Clear();
                tracePath.ForEach(node => traceQueue.Enqueue(node));
            }
            return this;
        }

        //returns null
        public NetworkNode GetCurrentTracingNode()
        {
            if (traceQueue.Count > 0)
                return traceQueue.Peek();
            else
                return null;
        }
        
        public bool IsRunning()
        {
            if (!blocked && traceQueue.Count > 0)
                return true;
            else return false;
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

            traceQueue.Clear();

            tracePath.ForEach(node => traceQueue.Enqueue(node));

            NetworkNode firstNode = traceQueue.Dequeue();

            nodeTraceAction = () =>
            {

                if (blocked)
                    return;
                
                if (traceQueue.Count > 1)
                {
                    consoleLog(String.Format("Tracer continued..."));

                    tracerAnimator
                        .SetStartPoint(traceQueue.Dequeue().GetPosition())
                        .SetEndPoint(traceQueue.Peek().GetPosition())
                        .SetHackingDuration(traceQueue.Peek().GetHackingDuration() * tracingSpeedModifier)
                        .Start(() => {
                            int tracerDelay = traceQueue.Peek().GetTracerDelay();
                            consoleLog(String.Format("Tracer delayed for {0} secs", tracerDelay));
                            timeoutWaiter.Wait(tracerDelay, nodeTraceAction);
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
                });
        }
        
    }
}
