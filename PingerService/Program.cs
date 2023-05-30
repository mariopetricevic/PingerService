using System.Net.NetworkInformation;
using k8s;
using k8s.Models;

namespace PingerService
{
    public class Program
    {
        static void Main(string[] args)
        {
            // Get cluster configuration
            var config = KubernetesClientConfiguration.InClusterConfig();
            IKubernetes client = new Kubernetes(config);

            //Get pod name and namespace from environment variables. Each pod deployment need to have these environment variables configured. 
            var podName = Environment.GetEnvironmentVariable("MY_POD_NAME");
            var namespaceName = Environment.GetEnvironmentVariable("MY_POD_NAMESPACE");

            //Crates instance of node pinger
            NodePinger nodePinger = new NodePinger(client, podName, namespaceName);

            while (true)
            {
                try
                {
                    nodePinger.GetCurrentNode(podName, namespaceName);
                    // Gets all nodes in cluster and their corresponding ip adresses and stores them in private dictionary
                    nodePinger.GetNodesAndAdrresses();
                    // Calculates distances from current node to each node in cluster 
                    nodePinger.CalculateNodeDistances();
                    // Prints labels and distances to each node
                    nodePinger.PrintNodeLabelsWithDistances();
                    // Updates labels of a current node with new calculated distances to each node in cluster
                    nodePinger.UpdateChangedDistances();

                    Thread.Sleep(15000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error occured: " + ex.Message);
                }
            }
        }
    }
}