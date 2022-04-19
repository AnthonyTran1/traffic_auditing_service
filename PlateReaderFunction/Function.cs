using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Util;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.S3.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PlateReaderFunction
{
    public class Function
    {
        IAmazonS3 S3Client { get; set; }

        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
        public Function()
        {
            S3Client = new AmazonS3Client();
        }

        /// <summary>
        /// Constructs an instance with a preconfigured S3 client. This can be used for testing the outside of the Lambda environment.
        /// </summary>
        /// <param name="s3Client"></param>
        public Function(IAmazonS3 s3Client)
        {
            this.S3Client = s3Client;
        }
        
        /// <summary>
        /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used 
        /// to respond to S3 notifications.
        /// </summary>
        /// <param name="evnt"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<string> FunctionHandler(S3Event evnt, ILambdaContext context)
        {
            var s3Event = evnt.Records?[0].S3;
            if(s3Event == null)
            {
                return null;
            }

            string downwardQueue = "https://sqs.us-east-1.amazonaws.com/900814902869/downwardQueueP3";
            try
            {
                var response = await this.S3Client.GetObjectMetadataAsync(s3Event.Bucket.Name, s3Event.Object.Key);
                Stream fileStream = await S3Client.GetObjectStreamAsync(s3Event.Bucket.Name, s3Event.Object.Key, null);


                Image licensePlate = GetImage(fileStream);
                AmazonRekognitionClient rekClient = new AmazonRekognitionClient();
                DetectTextRequest DTRequest = new DetectTextRequest
                {
                    Image = licensePlate
                };

                DetectTextResponse DTResponse = await rekClient.DetectTextAsync(DTRequest);

                string licensePlateNo = "";
                foreach (TextDetection line in DTResponse.TextDetections)
                {
                    if (line.DetectedText.Length == 7 && IsCapitalLettersAndNumbers(line.DetectedText.ToString()))
                    {
                        //may not need to string if doesnt work
                        licensePlateNo = line.DetectedText;
                    }    
                }
            
                bool isCalifornia = false;
                //checks if it's califonia
                foreach (TextDetection line in DTResponse.TextDetections)
                {
                    if(line.DetectedText.Contains("California", StringComparison.OrdinalIgnoreCase))
                    {
                        isCalifornia = true;
                    }
                }

                if (isCalifornia == false)
                {
                    CopyObjectRequest copyRequest = new CopyObjectRequest
                    {
                        SourceBucket = s3Event.Bucket.Name,
                        SourceKey = s3Event.Object.Key,
                        DestinationBucket = "anthony-bucket-project3-not-california",
                        DestinationKey = s3Event.Object.Key
                    };

                    CopyObjectResponse copyResponse = await S3Client.CopyObjectAsync(copyRequest);
                } else
                {
                    //get the information from tags
                    GetObjectTaggingRequest getTagsRequest = new GetObjectTaggingRequest
                    {
                        BucketName = s3Event.Bucket.Name,
                        Key = s3Event.Object.Key
                    };
                    GetObjectTaggingResponse objectTags = await S3Client.GetObjectTaggingAsync(getTagsRequest);

                    string location = objectTags.Tagging[0].Value;
                    string date = objectTags.Tagging[1].Value;
                    string type = objectTags.Tagging[2].Value;

                    string message = licensePlateNo + "," + location + "," + date + "," + type;
                    AmazonSQSClient sqsClient = new AmazonSQSClient();
                    SendMessageRequest sqsRequest = new SendMessageRequest
                    {
                        MessageBody = message,
                        QueueUrl = downwardQueue
                    };
                    Task<SendMessageResponse> downwardResponse = sqsClient.SendMessageAsync(sqsRequest);
                    downwardResponse.Wait();
                    if (downwardResponse.IsCompletedSuccessfully)
                    {
                        Console.WriteLine(message);
                        Console.WriteLine("Successfully sent to downwardQueue");
                    }
                }
                
                return response.Headers.ContentType;
            }
            catch(Exception e)
            {
                context.Logger.LogLine($"Error getting object {s3Event.Object.Key} from bucket {s3Event.Bucket.Name}. Make sure they exist and your bucket is in the same region as this function.");
                context.Logger.LogLine(e.Message);
                context.Logger.LogLine(e.StackTrace);
                throw;
            }
        }

        private Image GetImage(Stream s)
        {
            Image image = new Image();
            MemoryStream ms = new MemoryStream();
            s.CopyTo(ms);
            image.Bytes = ms;
            return image;
        }

        private static bool IsCapitalLettersAndNumbers(string s)
        {
            if (String.IsNullOrEmpty(s))
            {
                return false;
            }
            bool allDigits = true;
            foreach (char c in s)
            {
                if (!char.IsDigit(c))
                {
                    allDigits = false;
                    break;
                }
            }
            if (allDigits) return false;
            return System.Text.RegularExpressions.Regex.IsMatch(s, "^[A-Z0-9]*$");
        }
    }
}
