using k8s;
using k8s.Models;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Mozilla;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace PingerService
{
    /// <summary>
    /// Node pinger is intended to run as a DeamonSet on each node in a k3s cluster
    /// </summary>
    public class NodePinger
    {
        /// <summary>
        /// Kubernetes client to access resources in cluster
        /// </summary>
        private IKubernetes client;

        /// <summary>
        /// Dictionary contains each node name in a cluster and his IP address
        /// </summary>
        private Dictionary<string, string> nodeNameAddresssDictionary = new Dictionary<string, string>();

        /// <summary>
        /// Current node on which this program is run.
        /// </summary>
        private V1Node currentNode = null;

        /// <summary>
        /// Current node name
        /// </summary>
        private string currentNodeName = null;

        /// <summary>
        /// Public constrctor.
        /// </summary>
        /// <param name="client"></param>
        public NodePinger(IKubernetes client, string podName, string namespaceName)
        {
            this.client = client;

            //this.currentNode = GetCurrentNode(podName, namespaceName);
          
        }

        /// <summary>
        /// Calculates distance from current node to each other node in cluster using Ping.
        /// Stores results of Ping operation into corresponding labels of a current node
        /// Label format is: ping-[nameOfPingedNode] : pingRTT
        /// </summary>
        public void CalculateNodeDistances()
        {
            try
            {
                foreach (var node in nodeNameAddresssDictionary)
                {
                    if (node.Key != currentNodeName)
                    {
                        Ping ping = new Ping();
                        var averageRTT = CalculateAverageRTT(ping, node);

                        // If label does not exist, add it
                        if (!currentNode.Metadata.Labels.ContainsKey($"ping-{node.Key}"))
                        {
                            currentNode.Metadata.Labels.Add($"ping-{node.Key}", averageRTT.ToString());
                        }
                        else
                        {
                            currentNode.Metadata.Labels[$"ping-{node.Key}"] = averageRTT.ToString();
                        }
                    }
                }
            }
            catch(Exception e )
            {
                Console.WriteLine("Error occured while calculating node distances. " + e.ToString());
            }
           
        }

        /// <summary>
        /// Get all nodes in cluster and their addresses, store them in dictionary - key: nodeName, value: ip address
        /// </summary>
        public void GetNodesAndAdrresses()
        {
            try
            {
                V1NodeList listOfNodes = client.ListNode();
                foreach (var node in listOfNodes.Items)
                {
                    if (!nodeNameAddresssDictionary.ContainsKey(node.Name()))
                    {
                        nodeNameAddresssDictionary.Add(node.Name(), node.Status.Addresses[0].Address);
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Error occured while getting nodes and addresses. " + e.ToString());
            }
            
        }

        /// <summary>
        /// Prints node labels with corresponding distances.
        /// </summary>
        public void PrintNodeLabelsWithDistances()
        {
            try
            {
                Console.WriteLine("Printing node labels for current node: " + currentNodeName);
                foreach (var label in currentNode.Metadata.Labels)
                {
                    Console.WriteLine(label.Key + ": " + label.Value);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Error while printing. " + e.ToString()) ;
            }
         
        }

        /// <summary>
        /// Updates node in cluster with changed in memory current node.
        /// </summary>
        public void UpdateChangedDistances()
        {
            try
            {
                var node = client.ReplaceNodeStatus(currentNode, currentNodeName);
            }
            catch(Exception e)
            {
                this.currentNode = client.ReadNode(currentNodeName);

                var updateNode = client.ReplaceNodeStatus(currentNode, currentNode.Name());
                Console.WriteLine("Error occured while updating distances of nodes. " + e.ToString());
            }
        }

        /// <summary>
        /// Gets current node and current node name and stores them in private fields.
        /// </summary>
        /// <param name="podName"></param>
        /// <param name="namespaceName"></param>
        /// <returns></returns>
        public V1Node GetCurrentNode(string podName, string namespaceName)
        {
            var pod = client.ReadNamespacedPod(podName, namespaceName);
            this.currentNodeName = pod.Spec.NodeName;
            this.currentNode = client.ReadNode(currentNodeName);

            long distanceToCurrentNode = 0;
            // if current node does not contain label with currentNode name, add it with distance value 0
            if (!currentNode.Metadata.Labels.ContainsKey($"ping-{currentNodeName}"))
            {
                currentNode.Metadata.Labels.Add($"ping-{currentNodeName}", distanceToCurrentNode.ToString());
            }

            return currentNode;
        }

        /// <summary>
        /// Calculates average RTT for node.
        /// </summary>
        /// <param name="ping"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        private double CalculateAverageRTT(Ping ping, KeyValuePair<string, string> node)
        {
            try
            {
                long sum = 0;

                for (int i = 0; i < 3; i++)
                {
                    var pingReply = ping.Send(node.Value);
                    var pingResult = pingReply.Status == IPStatus.Success ? "Success" : "Failed";
                    Console.WriteLine("pinging node: " + node.Key + " address: " + node.Value + " RTT: " + pingReply.RoundtripTime.ToString());
                    sum += pingReply.RoundtripTime;
                }

                return sum / 3;
            }
            catch(Exception e)
            {
                Console.WriteLine("Error ocurred while pinging node: " + node.Key + " exception: " + e);
                return 100000;
            }
        }
    }
}
