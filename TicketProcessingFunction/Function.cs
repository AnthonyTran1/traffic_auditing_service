using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.Runtime;
using Amazon;
using Amazon.Runtime.CredentialManagement;
using Amazon.Translate;
using Amazon.Translate.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace TicketProcessingFunction
{
    public class Function
    {
        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
        public Function()
        {

        }


        /// <summary>
        /// This method is called for every Lambda invocation. This method takes in an SQS event object and can be used 
        /// to respond to SQS messages.
        /// </summary>
        /// <param name="evnt"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
        {
            foreach(var message in evnt.Records)
            {
                await ProcessMessageAsync(message, context);
            }
        }

        private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
        {
            try
            {
                context.Logger.LogLine($"Processed message {message.Body}");

                Console.WriteLine(message.Body);
                string[] messageArray = message.Body.Split(',');
                string plate = messageArray[0];
                string make = messageArray[1];
                string model = messageArray[2];
                string color = messageArray[3];
                string language = messageArray[4];
                string name = messageArray[5];
                string contact = messageArray[6];
                string location = messageArray[8];
                string date = messageArray[7];
                string type = messageArray[9];

                AmazonSimpleNotificationServiceClient SNSclient = new AmazonSimpleNotificationServiceClient();
                SubscribeRequest subscribeRequest = new SubscribeRequest
                {
                    Protocol = "email",
                    Endpoint = contact,
                    TopicArn = "arn:aws:sns:us-east-1:900814902869:TicketTopic"
                };
                Console.WriteLine("after sns client and sub request");
                SubscribeResponse subscribeResponse = await SNSclient.SubscribeAsync(subscribeRequest);

                string violationMessage = "Your vehicle was involved in a traffic violation. Please pay the specified ticket amount by 30 days: ";

                AmazonTranslateClient translateClient = new AmazonTranslateClient();

                string targetLangCode = "";

                if (language == "english")
                {
                    targetLangCode = "en";
                }
                else if (language == "spanish")
                {
                    targetLangCode = "es";
                }
                else if (language == "russian")
                {
                    targetLangCode = "ru";
                }
                else if (language == "french")
                {
                    targetLangCode = "fr";
                }

                string violationCost = "";
                if (type == "no_stop")
                {
                    violationCost = "$300.00";
                }
                else if (type == "no_full_stop_on_right")
                {
                    violationCost = "$75.00";
                }
                else if (type == "no_right_on_red")
                {
                    violationCost = "$125.00";
                }
                Console.WriteLine("after all the if checks");
                TranslateTextRequest translateRequest = new TranslateTextRequest
                {
                    SourceLanguageCode = "en",
                    TargetLanguageCode = targetLangCode,
                    Text = violationMessage,
                };

                TranslateTextResponse translateResponse = await translateClient.TranslateTextAsync(translateRequest);
                PublishRequest publishRequest = new PublishRequest
                {
                    Message = translateResponse.TranslatedText + "\n\n" +
                            "Vehicle: " + color + " " + make + model +
                            "\nLicense plate: " + plate +
                            "\nDate: " + date +
                            "\nViolation address: " + location +
                            "\nTicket type: " + type +
                            "\nTicket amount: " + violationCost,
                    TopicArn = "arn:aws:sns:us-east-1:900814902869:TicketTopic",
                    Subject = "IMPORTANT EMAIL NOT SPAM FR"

                };

                PublishResponse publishResponse = await SNSclient.PublishAsync(publishRequest);

            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            


            await Task.CompletedTask;
        }
        private static AWSCredentials GetAWSCredentialsByName(string profileName)
        {
            if (String.IsNullOrEmpty(profileName))
            {
                throw new ArgumentNullException("profileName cannot be null or empty");
            }

            SharedCredentialsFile credFile = new SharedCredentialsFile();
            CredentialProfile profile = credFile.ListProfiles().Find(p => p.Name.Equals(profileName));
            if (profile == null)
            {
                throw new Exception(String.Format("Profile named {0} not found", profileName));
            }
            return AWSCredentialsFactory.GetAWSCredentials(profile, new SharedCredentialsFile());
        }
    }
}
