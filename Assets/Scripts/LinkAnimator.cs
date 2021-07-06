using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace TwoDesperadosTest
{
    public class LinkAnimator
    {
        private Vector2 start;
        private Vector2 end;
        private Color color;

        private RectTransform gameObjectContainer;
        private MonoBehaviour coroutineHolder;

        private GameObject lineObject;
        private RectTransform lineTransform;

        private Vector2 dir;
        private float totalDistance;
        private float currentDistance;
        
        private float duration;
        private float stepLength;

        private bool running;
        private bool interrupted;
        

        public LinkAnimator(RectTransform container, MonoBehaviour coroutineHolder)
        {
            gameObjectContainer = container;
            this.coroutineHolder = coroutineHolder;
            this.color = Color.black;
            this.start = new Vector2();
            this.end = new Vector2();
            this.duration = 5f;
            this.running = false;
        }

        public LinkAnimator SetStartPoint(Vector2 start)
        {
            this.start = new Vector2(start.x, start.y);
            return this;
        }

        public LinkAnimator SetEndPoint(Vector2 end)
        {
            this.end = new Vector2(end.x, end.y);
            return this;
        }

        public LinkAnimator SetLinkColor(Color color)
        {
            this.color = color;
            return this;
        }

        public LinkAnimator SetHackingDuration(float durationInSecs)
        {
            if (durationInSecs < .1f)
                throw new ArgumentException(String.Format("Duration must be > 0.1 secs. Passed: {0}", durationInSecs));
            this.duration = durationInSecs;
            this.stepLength = totalDistance / (duration * 10);
            return this;
        }

        public float GetHackingDuration()
        {
            return duration;
        }
        
        public void Start()
        {
            Start(null);
        }

        public GameObject Start(Action action)
        {
            if (running)
                return null;

            currentDistance = 0f;
            totalDistance = Vector2.Distance(start, end);
            dir = (end - start).normalized;
            
            stepLength = totalDistance / (duration * 10);

            lineObject = new GameObject("Link Animation", typeof(Image));
            lineObject.transform.SetParent(gameObjectContainer, false);
            lineObject.GetComponent<Image>().color = this.color;
            
            lineTransform = lineObject.GetComponent<RectTransform>();
            lineTransform.anchorMin = new Vector2(0, 0);
            lineTransform.anchorMax = new Vector2(0, 0);
            lineTransform.sizeDelta = new Vector2(currentDistance, 3f); ;
            lineTransform.anchoredPosition = start + dir * currentDistance * 0.5f;

            lineTransform.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

            running = true;
            interrupted = false;

            coroutineHolder.StartCoroutine(DrawLineRoutine(action));

            return lineObject;
        }

        public void Stop()
        {
            this.interrupted = true;
        }

        public bool IsInterrupted()
        {
            return interrupted;
        }

        private IEnumerator DrawLineRoutine(Action action)
        {
            while (currentDistance < totalDistance)
            {
                if (interrupted)
                    break;
                
                currentDistance += stepLength;
                lineTransform.sizeDelta = new Vector2(currentDistance, 3f); //TODO garbage collection
                lineTransform.anchoredPosition = start + dir * currentDistance * 0.5f;
                yield return new WaitForSeconds(.1f);
            }

            if (!interrupted)
            {
                lineTransform.sizeDelta = new Vector2(totalDistance, 3f);
                lineTransform.anchoredPosition = start + dir * totalDistance * 0.5f;

                if (action != null)
                    action();
            }

            running = false;
            interrupted = false;
        }
    }
    
}
