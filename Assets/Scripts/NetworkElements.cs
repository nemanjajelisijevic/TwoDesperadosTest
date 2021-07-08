using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;



namespace TwoDesperadosTest
{
    public class NetworkNode
    {
        public enum Type
        {
            Start,
            Treasure,
            Firewall,
            Spam,
            Data
        }
        
        private Type type;
        private Vector2 position;
        private List<Link> links;
        private int hackingDificulty;
        private int tracerDelay;

        public const int MINIMUM_HACKING_DIFFICULTY = 4;

        public NetworkNode(Vector2 position, Type type)
        {
            this.position = position;
            this.type = type;
            this.hackingDificulty = 3;
            this.tracerDelay = 2;
            links = new List<Link>();
        }
        
        public Type GetNodeType()
        {
            return type;
        }

        public NetworkNode SetHackingDifficulty(int difficulty)
        {
            if (difficulty < MINIMUM_HACKING_DIFFICULTY || difficulty > 100)
                throw new ArgumentException(String.Format("Hacking difficulty must be >= {0} && <= 100", MINIMUM_HACKING_DIFFICULTY));

            hackingDificulty = difficulty;
            return this;
        }

        public int GetHackingDifficulty()
        {
            if (hackingDificulty == 0)
                throw new Exception("Hacking difficulty not set");
            return hackingDificulty;
        }

        public float GetHackingDuration()
        {
            return ((float)GetHackingDifficulty()) / MINIMUM_HACKING_DIFFICULTY;
        }

        public NetworkNode SetTracerDelay(int delay)
        {
            this.tracerDelay = delay;
            return this;
        }

        public int GetTracerDelay()
        {
            return tracerDelay;
        }

        public int GetNoOfLinks()
        {
            return links.Count();
        }

        public Vector2 GetPosition()
        {
            return position;
        }
        
        public NetworkNode AddLink(Link edge)
        {
            if (!links.Contains(edge))
                links.Add(edge);

            return this;
        }

        public NetworkNode RemoveLink(Link edge)
        {
            if (links.Contains(edge))
                links.Remove(edge);

            return this;
        }

        public List<NetworkNode> GetNieghbourNodes()
        {
            List<NetworkNode> ret = new List<NetworkNode>();

            links.ForEach(link => {

                if (link.GetNodes().Key.Equals(this))
                    ret.Add(link.GetNodes().Value);
                else if (link.GetNodes().Value.Equals(this))
                    ret.Add(link.GetNodes().Key);
                else
                    throw new Exception("Can not get neighbour node!");
            });

            return ret;
        }

        public Link GetLinkToNode(NetworkNode neighbor)
        {
            Link ret = null;

            for (int i = 0; i < links.Count; ++i)
            {
                if (links[i].GetNodes().Key.Equals(neighbor) || links[i].GetNodes().Value.Equals(neighbor))
                {
                    ret = links[i];
                    break;
                }
            }
            
            return ret;
        }

        public override string ToString()
        {
            return String.Format("Node - pos: {0}, Type: {1}, Hacking Difficulty: {2}, Tracer Delay: {3}", position, type.ToString(), hackingDificulty, tracerDelay);
        }
    }


    public class Link
    {
        private NetworkNode nodeOne;
        private NetworkNode nodeTwo;

        private float length;
        private Vector2 direction;

        private Link CalculateDirection()
        {
            direction = (nodeOne.GetPosition() - nodeTwo.GetPosition()).normalized;
            return this;
        }

        private Link CalculateLength()
        {
            length = Vector2.Distance(nodeOne.GetPosition(), nodeTwo.GetPosition());
            return this;
        }

        public float GetLinkLength()
        {
            return length;
        }

        public Vector2 GetDirection()
        {
            return direction;
        }

        public Link SetNodes(NetworkNode nodeOne, NetworkNode nodeTwo)
        {
            if (nodeOne != null && nodeTwo != null)
            {
                this.nodeOne = nodeOne;
                this.nodeTwo = nodeTwo;
                CalculateLength();
                CalculateDirection();
            }
            else
            {
                throw new ArgumentException("Both nodes must be != null");
            }

            return this;
        }

        public KeyValuePair<NetworkNode, NetworkNode> GetNodes()
        {
            return new KeyValuePair<NetworkNode, NetworkNode>(nodeOne, nodeTwo);
        }

        public Link(NetworkNode nodeOne, NetworkNode nodeTwo)
        {
            SetNodes(nodeOne, nodeTwo);
        }
        
        public void Disconnect()
        {
            nodeOne.RemoveLink(this);
            nodeTwo.RemoveLink(this);
            nodeOne = null;
            nodeTwo = null;
        }

        public bool IsConnectedToNode(NetworkNode node)
        {
            return (node.Equals(nodeOne) || node.Equals(nodeTwo));
        }

        //for List comparisons
        public override bool Equals(object obj)
        {
            var edge = obj as Link;
            return edge != null &&
                   (
                   (EqualityComparer<NetworkNode>.Default.Equals(nodeOne, edge.nodeOne) &&
                   EqualityComparer<NetworkNode>.Default.Equals(nodeTwo, edge.nodeTwo)) 
                   ||
                   (EqualityComparer<NetworkNode>.Default.Equals(nodeOne, edge.nodeTwo) &&
                   EqualityComparer<NetworkNode>.Default.Equals(nodeTwo, edge.nodeOne))
                   //&& length.Equals(edge.EdgeLength())
                   );
        }


        public static float GetAngleBetweenLinks(Link one, Link two)
        {
            float angleOne = Mathf.Atan2(one.GetDirection().y, one.GetDirection().x) * Mathf.Rad2Deg;
            float angleTwo = Mathf.Atan2(two.GetDirection().y, two.GetDirection().x) * Mathf.Rad2Deg;
            return Math.Abs(angleTwo - angleOne);

            //Vector2 res = two.GetDirection() - one.GetDirection();
            //return Math.Abs(Mathf.Atan2(res.y, res.x) * Mathf.Rad2Deg);

            //return Vector2.Angle(two.GetDirection(), one.GetDirection());
        }

    }

    //Can be implemented with different triangulation algorithms (i.e. Dalaunay)
    public interface ILinkGenerator
    {
        List<Link> GenerateLinks(List<NetworkNode> nodes);
    }

    public class IncrementalTriangulationLinkGenerator : ILinkGenerator
    {
        public List<Link> GenerateLinks(List<NetworkNode> nodes)
        {
            if (nodes.Count < 3)
            {
                throw new ArgumentException(
                    String.Format(
                        "Node list must have at least 3 elements. Actual no of elements {0}",
                        nodes.Count
                        ));
            }

            List<Link> links = new List<Link>();

            //sort nodes by x coordinate
            nodes = nodes.OrderBy(node => node.GetPosition().x).ToList();

            //add edges of the first triangle
            Link link01 = new Link(nodes[0], nodes[1]);
            nodes[0].AddLink(link01);
            nodes[1].AddLink(link01);
            links.Add(link01);

            Link link12 = new Link(nodes[1], nodes[2]);
            nodes[1].AddLink(link12);
            nodes[2].AddLink(link12);
            links.Add(link12);

            Link link20 = new Link(nodes[2], nodes[0]);
            nodes[2].AddLink(link20);
            nodes[0].AddLink(link20);
            links.Add(link20);


            for (int i = 3; i < nodes.Count; ++i)
            {
                NetworkNode currentNode = nodes[i];

                List<Link> newLinks = new List<Link>();

                for (int j = 0; j < links.Count; ++j)
                {
                    Link currentLink = links[j];

                    Vector2 currentLinkMidPoint = (currentLink.GetNodes().Key.GetPosition() + currentLink.GetNodes().Value.GetPosition()) / 2f;

                    NetworkNode midpointNode = new NetworkNode(currentLinkMidPoint, NetworkNode.Type.Data);

                    Link linkToMidPointNode = new Link(currentNode, midpointNode);

                    bool canSeeLink = true;

                    for (int k = 0; k < links.Count; ++k)
                    {
                        if (k == j)
                            continue;

                        if (IntersectingEdges(linkToMidPointNode, links[k]))
                        {
                            canSeeLink = false;
                            break;
                        }
                    }

                    if (canSeeLink)
                    {
                        NetworkNode node1 = currentLink.GetNodes().Key;
                        NetworkNode node2 = currentLink.GetNodes().Value;

                        Link linkToNode1 = new Link(node1, currentNode);
                        Link linkToNode2 = new Link(node2, currentNode);

                        if (!newLinks.Contains(linkToNode1))
                        {
                            node1.AddLink(linkToNode1);
                            currentNode.AddLink(linkToNode1);
                            newLinks.Add(linkToNode1);
                        }

                        if (!newLinks.Contains(linkToNode2))
                        {
                            node2.AddLink(linkToNode2);
                            currentNode.AddLink(linkToNode2);
                            newLinks.Add(linkToNode2);
                        }
                    }

                }

                newLinks.ForEach(edge => {
                    if (!links.Contains(edge))
                        links.Add(edge);
                    else
                        throw new Exception("Triangulation - edge already contained in edge list");
                });

            }

            return links;
        }

        private static bool IntersectingEdges(Link one, Link two)
        {
            Vector2 l1_p1 = new Vector2(one.GetNodes().Key.GetPosition().x, one.GetNodes().Key.GetPosition().y);
            Vector2 l1_p2 = new Vector2(one.GetNodes().Value.GetPosition().x, one.GetNodes().Value.GetPosition().y);
            Vector2 l2_p1 = new Vector2(two.GetNodes().Key.GetPosition().x, two.GetNodes().Key.GetPosition().y);
            Vector2 l2_p2 = new Vector2(two.GetNodes().Value.GetPosition().x, two.GetNodes().Value.GetPosition().y);

            return IntersectingLines(l1_p1, l1_p2, l2_p1, l2_p2, true);
        }

        private static bool IntersectingLines(Vector2 l1_p1, Vector2 l1_p2, Vector2 l2_p1, Vector2 l2_p2, bool shouldIncludeEndPoints)
        {
            bool isIntersecting = false;

            float denominator = (l2_p2.y - l2_p1.y) * (l1_p2.x - l1_p1.x) - (l2_p2.x - l2_p1.x) * (l1_p2.y - l1_p1.y);

            //Make sure the denominator is > 0, if not the lines are parallel
            if (denominator != 0f)
            {
                float u_a = ((l2_p2.x - l2_p1.x) * (l1_p1.y - l2_p1.y) - (l2_p2.y - l2_p1.y) * (l1_p1.x - l2_p1.x)) / denominator;
                float u_b = ((l1_p2.x - l1_p1.x) * (l1_p1.y - l2_p1.y) - (l1_p2.y - l1_p1.y) * (l1_p1.x - l2_p1.x)) / denominator;

                //Are the line segments intersecting if the end points are the same
                if (shouldIncludeEndPoints)
                {
                    //Is intersecting if u_a and u_b are between 0 and 1 or exactly 0 or 1
                    if (u_a >= 0f && u_a <= 1f && u_b >= 0f && u_b <= 1f)
                    {
                        isIntersecting = true;
                    }
                }
                else
                {
                    //Is intersecting if u_a and u_b are between 0 and 1
                    if (u_a > 0f && u_a < 1f && u_b > 0f && u_b < 1f)
                    {
                        isIntersecting = true;
                    }
                }

            }

            return isIntersecting;
        }
    }

}