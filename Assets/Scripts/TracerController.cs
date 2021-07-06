﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace TwoDesperadosTest
{

    public class TracerController
    {
        private static int tracerCount = 0;
        private int tracerNumber;

        private LinkAnimator tracerAnimator;
        private TimeoutWaiter timeoutWaiter;
        private Queue<NetworkNode> traceQueue;
        private NetworkNode currentTracingNode = null;

        private float tracingSpeedModifier;
        private bool blocked;

        private Action nodeTraceAction = null;
        private Action traceCompletedAction = null;

        public Action<string> consoleLog = null;

        public TracerController(LinkAnimator linkAnimator, TimeoutWaiter timeoutWaiter)
        {
            tracerCount++;
            this.tracerNumber = tracerCount;
            this.tracerAnimator = linkAnimator;
            this.timeoutWaiter = timeoutWaiter;
            this.tracingSpeedModifier = 1f;
            this.traceQueue = new Queue<NetworkNode>();
            this.blocked = false;
        }

        //trace complete action setter
        public TracerController SetTraceCompletedAction(Action traceCompletedAction)
        {
            this.traceCompletedAction = traceCompletedAction;
            return this;
        }

        //to be called from settings
        public static void ValidateTracerDecreaseSpeed(int percent)
        {
            if (percent < 1 || percent > 99)
                throw new ArgumentException(String.Format("Decrease tracer speed percent must be > 1 && < 100"));
        }

        public TracerController DecreaseTracerSpeed(int percent)
        {
            ValidateTracerDecreaseSpeed(percent);

            float newPercent = 100 - percent;
            tracingSpeedModifier = newPercent / 100f;

            //if (IsActive()) 
            //    tracerAnimator.SetHackingDuration(traceQueue.Peek().GetHackingDuration() / tracingSpeedModifier);
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
        
        public bool IsActive()
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
                    consoleLog(String.Format("Tracer {0} is blocked!", tracerNumber));
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
                    consoleLog(String.Format("Tracer {0] continued...", tracerNumber));

                    tracerAnimator
                        .SetStartPoint(traceQueue.Dequeue().GetPosition())
                        .SetEndPoint(traceQueue.Peek().GetPosition())
                        .SetHackingDuration(traceQueue.Peek().GetHackingDuration() / tracingSpeedModifier)
                        .Start(() => {
                            int tracerDelay = traceQueue.Peek().GetTracerDelay();
                            consoleLog(String.Format("Tracer {0} delayed for {1} secs", tracerNumber, tracerDelay));
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
                .SetHackingDuration(traceQueue.Peek().GetHackingDuration() / tracingSpeedModifier)
                .Start(() => {
                    int tracerDelay = traceQueue.Peek().GetTracerDelay();
                    consoleLog(String.Format("Tracer {0} delayed for {1} secs", tracerNumber, tracerDelay));
                    timeoutWaiter.Wait(tracerDelay, nodeTraceAction);
                });
        }
        
    }
}
