/*
 * Program:         Project2_Wordle
 * File:            Program.cs
 * Date:            March 13, 2025
 * Author:          Jazz Leibur, 0690432, Section 2 & Alec Lahey, 1056146, Section 2
 * Description:     Holds code to run DailyWordService WCF server (.NET Framework).
 */

using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using WordServer.Services;

namespace WordServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Create a URI to serve as the base address
            Uri baseAddress = new Uri("http://localhost:8080/DailyWordService");

            // Create the ServiceHost
            using (ServiceHost host = new ServiceHost(typeof(DailyWordService), baseAddress))
            {
                try
                {
                    // Add a service endpoint
                    host.AddServiceEndpoint(
                        typeof(IDailyWordService),
                        new BasicHttpBinding(),
                        "");

                    // Enable metadata exchange
                    ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
                    smb.HttpGetEnabled = true;
                    host.Description.Behaviors.Add(smb);

                    // Open the ServiceHost to start listening for messages
                    host.Open();

                    Console.WriteLine("The DailyWord service is ready at {0}", baseAddress);
                    Console.WriteLine("WSDL available at {0}?wsdl", baseAddress);
                    Console.WriteLine("Press <Enter> to stop the service.");
                    Console.ReadLine();

                    // Close the ServiceHost
                    host.Close();
                }
                catch (CommunicationException ce)
                {
                    Console.WriteLine("An exception occurred: {0}", ce.Message);
                    host.Abort();
                }
            }
        }
    }
}