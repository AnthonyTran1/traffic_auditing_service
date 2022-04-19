using System;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using System.Collections.Generic;
using System.Linq;

namespace UploadData
{
    class Program
    {
        private const string bucketName = "anthony-bucket-project3-california";
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            string[] addresses = { "123 Street NE and 456 Street SW intersection - Seattle WA 98108",
                                "534 Street S and 344 Street W intersection - Bellevue WA 98156",
                                "1st Street S and 2nd Street N intersection - Renton WA 98156"};

            string[] type = {"no_stop", "no_full_stop_on_right", "no_right_on_red"};
            Random r = new Random();

            if (Environment.GetCommandLineArgs().Length != 2)
            {
                Console.WriteLine("ERROR: Format should be: 'APP' 'FILE_PATH'!");
            }
            else
            {
                string[] filePathArray = Environment.GetCommandLineArgs().ElementAt(1).Split('\\');
                string fileName = filePathArray[filePathArray.Length - 1];
                string filePath = Environment.GetCommandLineArgs().ElementAt(1);

                //get credentials to use to authenticate ourselves to aws
                AWSCredentials credentials = GetAWSCredentialsByName("default");

                //get an object that allows us to interact with some aws service
                AmazonS3Client s3Client = new AmazonS3Client(credentials, RegionEndpoint.USEast1);

                var putRequest2 = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = fileName,
                    FilePath = filePath,
                    TagSet = new List<Tag>{
                        new Tag { Key = "location", Value = addresses[r.Next(0,3)]},
                        new Tag { Key = "dateTime", Value = DateTime.Now.ToString()},
                        new Tag { Key = "type", Value = type[r.Next(0,3)]}
                    }
                };
                PutObjectResponse response2 = await s3Client.PutObjectAsync(putRequest2);
            }
        }
        //get aws credentials by profile name
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
