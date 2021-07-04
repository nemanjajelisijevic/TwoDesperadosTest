using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;


namespace TwoDesperadosTest
{

    public interface IPathFinder
    {
        List<NetworkNode> FindShortestPath(NetworkNode sourceNode, NetworkNode endNode);
    }

    public class DijkstraPathFinder : IPathFinder
    {
        public class NetworkNodeWrapper
        {
            internal NetworkNode node;
            internal int cumulatedDifficulty;
            internal List<NetworkNode> shortestPathFromSource;

            public NetworkNodeWrapper(NetworkNode node)
            {
                this.node = node;
                this.cumulatedDifficulty = Int32.MaxValue;
                this.shortestPathFromSource = new List<NetworkNode>();
            }

            public override bool Equals(object obj)
            {
                var wrapper = obj as NetworkNodeWrapper;
                return wrapper != null &&
                       EqualityComparer<NetworkNode>.Default.Equals(node, wrapper.node);
            }

            public override int GetHashCode()
            {
                return -231681771 + EqualityComparer<NetworkNode>.Default.GetHashCode(node);
            }

            public override string ToString()
            {
                return String.Format("Node: {0} , cumulatedDiff: {1}", node, cumulatedDifficulty);
            }
        }

        public List<NetworkNode> FindShortestPath(NetworkNode sourceNode, NetworkNode endNode)
        {
            NetworkNodeWrapper ret = null;

            NetworkNodeWrapper source = new NetworkNodeWrapper(sourceNode);
            source.cumulatedDifficulty = 0;

            HashSet<NetworkNodeWrapper> visitedNodes = new HashSet<NetworkNodeWrapper>();
            HashSet<NetworkNodeWrapper> unvisitedNodes = new HashSet<NetworkNodeWrapper>();

            unvisitedNodes.Add(source);

            while (unvisitedNodes.Count > 0)
            {
                NetworkNodeWrapper currentNode = GetLowestDifficultyNode(unvisitedNodes);

                //Debug.LogFormat("Current node: {0}", currentNode.ToString());

                unvisitedNodes.Remove(currentNode);

                List<NetworkNodeWrapper> adjacentNodes = new List<NetworkNodeWrapper>();

                foreach (NetworkNode adjNode in currentNode.node.GetNieghbourNodes())
                {
                    NetworkNodeWrapper adjNodeWrapper = new NetworkNodeWrapper(adjNode); //TODO Bug is here!!!!!!!!!
                    adjacentNodes.Add(adjNodeWrapper);
                }
                
                foreach (NetworkNodeWrapper adjNodeWrapper in adjacentNodes)
                {
                    Debug.Log(adjNodeWrapper);

                    if (!visitedNodes.Contains(adjNodeWrapper))
                    {
                        CalculateMinDiffPathToNode(adjNodeWrapper, currentNode);
                        bool added = unvisitedNodes.Add(adjNodeWrapper);
                    }

                }

                visitedNodes.Add(currentNode);

                if (currentNode.node.Equals(endNode))
                {
                    ret = currentNode;
                    ret.shortestPathFromSource.Add(ret.node);
                }
            }

            return ret.shortestPathFromSource;
           
        }

        private NetworkNodeWrapper GetLowestDifficultyNode(HashSet<NetworkNodeWrapper> nodeSet)
        {
            NetworkNodeWrapper lowestDifficultyNode = null;
            int lowestDifficulty = Int32.MaxValue;

            foreach (NetworkNodeWrapper nodeWrapper in nodeSet)
            {
                int nodeDifficulty = nodeWrapper.cumulatedDifficulty;
                if (nodeDifficulty < lowestDifficulty)
                {
                    lowestDifficulty = nodeDifficulty;
                    lowestDifficultyNode = nodeWrapper;
                }
            }

            return lowestDifficultyNode;
        }

        private void CalculateMinDiffPathToNode(NetworkNodeWrapper evaluatedNode, NetworkNodeWrapper sourceNode)
        {
            int sourceCumulatedDifficulty = sourceNode.cumulatedDifficulty;

            if (sourceCumulatedDifficulty + evaluatedNode.node.GetHackingDifficulty() < evaluatedNode.cumulatedDifficulty)
            {
                evaluatedNode.cumulatedDifficulty = sourceCumulatedDifficulty + evaluatedNode.node.GetHackingDifficulty();
                List<NetworkNode> shortestPath = new List<NetworkNode>(sourceNode.shortestPathFromSource);
                shortestPath.Add(sourceNode.node);
                evaluatedNode.shortestPathFromSource = shortestPath;
            }
        }

    }
}
