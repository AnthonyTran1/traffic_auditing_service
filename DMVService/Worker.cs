using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace DMVService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private const string logPath = @"C:\Users\Antho\Desktop\project2_log\polling.log";

        private const string accessKey = "ASIA5DPGASZKXSRXSYEQ";
        private const string secretKey = "HdOuKKQFRLd/ojr1pwKmb+BRLs1yA1QubXG8n+gF";
        private const string token = "FwoGZXIvYXdzEML//////////wEaDFcud9h2Kc+bU7O93CLSAdEApa7dOGawB7gG3moqe6HKwxR3t7/JHdkQYgMS7kzV5ai69W/+6uTLYwkw07dObSaEosVYmQY0zKKPnmqHKxUoOqAgxc3T06wWKcTSTtmSAm4TnA/1QlbB9Wf7c5y4qusooV9zIUuMPPFRB5KjKLjuCEHJlg1tSgJtqJKOj2orwJoh4sXvm+4bSpcK/8VEOapD0yHMqRvib527IbmUjMxOGPvSyMCI9Szl5dtytLhl6PTkqZ/6+0EiNiq/kqDE+iWCNhYqyv8xntVyAdPHHvp6GiijjbOGBjItEumkfK02Nl8ylyQmMihOFFaM5XPU0fhzu5Q0wUtf8g7aibIs2l7yiCaHABRf";
        private const string downwardQueue = "https://sqs.us-east-1.amazonaws.com/900814902869/downwardQueueP3";
        private const string upwardQueue = "https://sqs.us-east-1.amazonaws.com/900814902869/upwardQueueP3";

        private string plate = "";
        private string make = "";
        private string model = "";
        private string color = "";
        private string language = "";
        private string name = "";
        private string contact = "";

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            try
            {
                CredentialProfileOptions options = new CredentialProfileOptions()
                {
                    AccessKey = accessKey,
                    SecretKey = secretKey,
                    Token = token
                };
                AWSCredentials credentials = AWSCredentialsFactory.GetAWSCredentials(options, null);
                AmazonSQSClient amazonSQSClient = new AmazonSQSClient(credentials, RegionEndpoint.USEast1);

                while (!stoppingToken.IsCancellationRequested)
                {
                    //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    var request = new ReceiveMessageRequest
                    {
                        QueueUrl = downwardQueue,
                        WaitTimeSeconds = 20
                    };

                    var response = await amazonSQSClient.ReceiveMessageAsync(request);

                    string text = string.Format("{0}:\tRead message:\tfinish await for response", DateTime.Now);
                    using (StreamWriter writer = new StreamWriter(logPath, append: true))
                    {
                        writer.WriteLine(text);
                    }

                    if (response.Messages.Count > 0)
                    {
                        foreach(var message in response.Messages)
                        {

                            string downwardMessage = message.Body;
                            text = string.Format("{0}:\tRead message:\tentered foreach\t{1}", DateTime.Now,downwardMessage);
                            using (StreamWriter writer = new StreamWriter(logPath, append: true))
                            {
                                writer.WriteLine(text);
                            }


                            string[] messageArray = message.Body.Split(',');
                            string licensePlateNo = messageArray[0];
                            string location = messageArray[1];
                            string date = messageArray[2];
                            string type = messageArray[3];

                            XmlDocument xmlDoc = new XmlDocument();
                            xmlDoc.Load(@"C:\Users\Antho\Desktop\project3_folder\DMVDatabase.xml");
                            XmlElement root = xmlDoc.DocumentElement;
                            XmlNode queryID = root.SelectSingleNode("vehicle[@plate=\"" + licensePlateNo + "\"]");
                            if (queryID != null)
                            {
                                text = string.Format("{0}:\tRead message:\tloaded xml", DateTime.Now);
                                using (StreamWriter writer = new StreamWriter(logPath, append: true))
                                {
                                    writer.WriteLine(text);
                                }
                                plate = queryID.Attributes[0].Value;
                                XmlNode makeNode = queryID.SelectSingleNode("make");
                                make = makeNode.InnerText;
                                XmlNode modelNode = queryID.SelectSingleNode("model");
                                model = modelNode.InnerText;
                                XmlNode colorNode = queryID.SelectSingleNode("color");
                                color = colorNode.InnerText;
                                XmlNode ownerNode = queryID.SelectSingleNode("owner");
                                language = ownerNode.Attributes[0].Value;
                                XmlNode nameNode = ownerNode.SelectSingleNode("name");
                                name = nameNode.InnerText;
                                XmlNode contactNode = ownerNode.SelectSingleNode("contact");
                                contact = contactNode.InnerText;

                                //delete previous message
                                var deleteRequest = new DeleteMessageRequest()
                                {
                                    QueueUrl = downwardQueue,
                                    ReceiptHandle = message.ReceiptHandle
                                };
                                var delResponse = await amazonSQSClient.DeleteMessageAsync(deleteRequest);

                                text = string.Format("{0}:\tRead message:\tdeleted message", DateTime.Now);
                                using (StreamWriter writer = new StreamWriter(logPath, append: true))
                                {
                                    writer.WriteLine(text);
                                }

                                //send new message to upwardQueue
                                var upwardRequest = new SendMessageRequest
                                {
                                    MessageBody = plate + "," + make + "," + model + "," + color + "," + language + "," + name + "," + contact + "," + location + "," + date + "," + type,
                                    QueueUrl = upwardQueue
                                };

                                Task<SendMessageResponse> upwardResponse = amazonSQSClient.SendMessageAsync(upwardRequest);
                                upwardResponse.Wait();
                                if (upwardResponse.IsCompletedSuccessfully)
                                {
                                    text = string.Format("{0}:\tRead message:\tsent to upwardqueue with this message {1}", DateTime.Now , plate + "," + make + "," + model + "," + color + "," + language + "," + name + "," + contact + "," + location + "," + date + "," + type);
                                    using (StreamWriter writer = new StreamWriter(logPath, append: true))
                                    {
                                        writer.WriteLine(text);
                                    }
                                }
                     
                            }
                        }  
                    }

                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                string text = string.Format("{0}:\tRead error message:\t{1}", DateTime.Now, e.Message);
                using (StreamWriter writer = new StreamWriter(logPath, append: true))
                {
                    writer.WriteLine(text);
                }
            }
        }
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            //do here anything you want to do when the service starts

            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancelllationToken)
        {
            //do here anything you want to do when the service stops

            await base.StopAsync(cancelllationToken);
        }
        public void WriteToLog(string message)
        {
            string text = string.Format("{0}:\tRead message:\t{1}", DateTime.Now, message);
            using (StreamWriter writer = new StreamWriter(logPath, append: true))
            {
                writer.WriteLine(text);
            }
        }
    }
}
