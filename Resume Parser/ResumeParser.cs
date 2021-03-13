/* 
 * ResumeParser.cs
 * 
 * @contributors: Kolby Samson (@kosamson), Jorge Alvarez (@J-Alv)
 * @purpose: Wrapper class containing functionality related to the usage
 *           of the Affinda Resume Parser API. Handles the uploading and
 *           retrieval of resume files and their respective parsing JSON
 *           responses. Also responsible for the deserialization of the
 *           received parsed data within a JSON object.
 */

using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using Newtonsoft.Json;

namespace Resume_Parser
{
    class ResumeParser
    {
        private static readonly string AFFINDA_API_KEY = "";
        private static readonly string AFFINDA_API_BASEURL = "https://resume-parser.affinda.com/public/api/v1/documents/";

        private ResumeRoot resumeRoot;
        public string fileID;
        public string resumeJSON;

        // Initializes a new ResumeParser object from an input uploaded file
        // and calls the respective Affinda API endpoints to upload and parse
        // the resume on their servers.
        // Deserializes the response JSON object containing the parsed data
        public ResumeParser(string fileName)
        {
            // Upload resume to the Affinda servers
            // and retrieve the associated file ID
            Task upload = UploadFileToAffinda();

            if (fileID == null)
                throw new Exception($"ERROR: Could not upload file: {fileName} to Affinda's API.");

            // Download the JSON parsed data file from Affinda
            // using the associated file ID
            Task download = ParseFileOnAffinda(fileID);

            // Inner-JSON object null check to ensure the data was not corrupted
            // or had errors while sent to Affinda
            int name = resumeJSON.IndexOf("name");
            if (resumeJSON.Substring(name + 6, 4) == "null")
            {
                throw new Exception($"ERROR: Could not parse file: {fileName} on Affinda's API.");
            }

            if (resumeJSON == null)
                throw new Exception($"ERROR: Could not parse file: {fileName} on Affinda's API.");

            // Ignore any null or missing members before deserialization
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            // Convert JSON text into a deserialized JSON object
            resumeRoot = JsonConvert.DeserializeObject<ResumeRoot>(resumeJSON, settings);
        }

        // Alternative constructor for the Resume Parser that skips the
        // upload step in the parsing process if it has already been
        // identified by the fileID (parameter) corresponding to a file
        // already uploaded and parsed on the Affinda API
        public ResumeParser(string fileName, string fileID)
        {
            // Download the JSON parsed data file from Affinda
            // using the associated file ID
            Task download = ParseFileOnAffinda(fileID);

            // Inner-JSON object null check to ensure the data was not corrupted
            // or had errors while sent to Affinda
            int name = resumeJSON.IndexOf("name");
            if (resumeJSON.Substring(name + 6, 4) == "null")
            {
                throw new Exception($"ERROR: Could not parse file: {fileName} on Affinda's API.");
            }

            if (resumeJSON == null)
                throw new Exception($"ERROR: Could not parse file: {fileName} on Affinda's API.");

            // Ignore any null or missing members before deserialization
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            // Convert JSON text into a deserialized JSON object
            resumeRoot = JsonConvert.DeserializeObject<ResumeRoot>(resumeJSON, settings);
        }

        // Utility function for the Resume Parser to upload the associated
        // file from the website to Affinda's API and retrieve its file ID
        private async Task UploadFileToAffinda()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AFFINDA_API_KEY);

            // Retrieve file content from our blob storage 
            var content = new StringContent("{\"url\":\"https://p5blobstorage.blob.core.windows.net/stringparser/input.pdf\"}",
                Encoding.UTF8, "application/json");

            int sleepTime = 0;
            int backOff = 0;

            Task<HttpResponseMessage> response = null;
            string responseStr = null;

            // Repeat API call to upload file until the response
            // will take longer than 15 seconds to wait for
            // due to exponential backoff
            while (sleepTime <= 6400)
            {
                Thread.Sleep(sleepTime);

                // Make upload request
                response = client.PostAsync(AFFINDA_API_BASEURL, content);

                // Increment backoff and exit if failed too many times on any other code
                if (!response.Result.IsSuccessStatusCode)
                {
                    if (sleepTime >= 6400)
                        return;

                    sleepTime = (int)Math.Pow(2, backOff) * 100;
                    backOff++;
                    continue;
                }

                // Read back the HTTP response as a string
                responseStr = response.Result.Content.ReadAsStringAsync().Result;
                break;
            }

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            // Deserialize the JSON response and retrieve the file ID
            var uploadJSON = JsonConvert.DeserializeObject<UploadRoot>(responseStr, settings);
            fileID = uploadJSON.identifier;
        }

        // Utility function to retrieve the parsed data from the Affinda API
        // and deserialize its JSON into the actual JSON objects contained
        // within this file as accessible classes
        public async Task ParseFileOnAffinda(string fileID)
        {
            //create client
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AFFINDA_API_KEY);

            //start time of this method's call
            DateTime start = DateTime.UtcNow;

            //sleep time used by loop to apply exponential backoff
            int sleepTime = 0;
            //backoff represents the amount of retries attempted
            int backOff = 0;

            //loop while waiting for response from API
            while(true)
            {
                //sleep depending on amount dictated by exponential backoff
                Thread.Sleep(sleepTime);

                //call on API to get parsed resume
                var response = client.GetAsync((AFFINDA_API_BASEURL + fileID));

                // Increment backoff and exit if failed too many times on any other code
                if (!response.Result.IsSuccessStatusCode)
                {
                    if (sleepTime >= 6400)
                        return;

                    sleepTime = (int)Math.Pow(2, backOff) * 100;
                    backOff++;
                    continue;
                }

                //turn the response into a string to be read
                string responseStr = response.Result.Content.ReadAsStringAsync().Result;

                //look for the meta status "ready" which represents if the document has been parsed by Affinda
                int ready = responseStr.IndexOf("ready");

                //end time and lap time to see how long it has been
                DateTime end = DateTime.UtcNow;
                TimeSpan lap = end - start;

                //if the status of the doc's parsing is still false, update sleeptime and continue
                if(responseStr.Substring(ready + 7, 5) == "false")
                {
                    sleepTime = (int)Math.Pow(2, backOff) * 100;
                    backOff++;
                    continue;
                }

                //if we've timed out (exceeded 10 seconds of wait) update resumeJSON so caller can make a decision
                else if(Convert.ToInt32(lap.TotalSeconds) > 15)
                {
                    resumeJSON = responseStr;
                    return;
                }

                //if doc has been parsed (ready status = true) update the resumeJSON string
                else
                {
                    resumeJSON = responseStr;
                    return;
                }        
            }
            
        }


        // #####################################
        // ## JSON Object Retrieval Functions ##
        // #####################################


        private Data GetData()
        {
            if (resumeRoot == null)
                return null;
            
            return resumeRoot.data;
        }

        private Name GetNameObject()
        {
            var data = GetData();

            if (data == null)
                return null;
            
            return data.name;
        }
        public string GetName()
        {
            Name nameObject = GetNameObject();

            if (nameObject == null)
                return "<i>MISSING</i>";

            return nameObject.title + nameObject.first + " " + nameObject.middle + " " + nameObject.last;
        }

        public string[] GetEmails()
        {
            var data = GetData();

            if (data == null)
                return new string[] { "<i>MISSING</i>" };

            return data.emails;
        }

        public string[] GetPhones()
        {
            var data = GetData();

            if (data == null)
                return new string[] { "<i>MISSING</i>" };

            return data.phoneNumbers;
        }

        public Education[] GetEducationHistory()
        {
            var data = GetData();

            if (data == null)
                return null;

            return data.education;
        }

        public Workexperience[] GetWorkExperience()
        {
            var data = GetData();

            if (data == null)
                return null;

            return data.workExperience;
        }

        public string[] GetSkills()
        {
            var data = GetData();

            if (data == null)
                return null;

            return data.skills;
        }

        public string[] GetLinks()
        {
            var data = GetData();

            if (data == null)
                return null;

            return data.websites;
        }

        public Section[] GetSections()
        {
            var data = GetData();

            if (data == null)
                return null;

            return data.sections;
        }

        public List<string> GetSectionNames()
        {
            Section[] sections = GetSections();

            if (sections == null)
                return new List<string>();

            var sectionNames = new List<string>();

            foreach (Section section in sections)
            {
                sectionNames.Add(section.sectionType);
            }

            return sectionNames;
        }

    }
    public class ResumeRoot
    {
        public Data data { get; set; }
        public Meta meta { get; set; }
        public Error error { get; set; }
    }

    public class Data
    {
        public object[] certifications { get; set; }
        public object dateOfBirth { get; set; }
        public Education[] education { get; set; }
        public string[] emails { get; set; }
        public object location { get; set; }
        public Name name { get; set; }
        public string objective { get; set; }
        public string[] phoneNumbers { get; set; }
        public object[] publications { get; set; }
        public object[] referees { get; set; }
        public Section[] sections { get; set; }
        public string[] skills { get; set; }
        public string summary { get; set; }
        public string[] websites { get; set; }
        public Workexperience[] workExperience { get; set; }
    }

    public class Name
    {
        public string raw { get; set; }
        public string first { get; set; }
        public string last { get; set; }
        public string middle { get; set; }
        public string title { get; set; }
    }

    public class Education
    {
        public string organization { get; set; }
        public Accreditation accreditation { get; set; }
        public Grade grade { get; set; }
        public Location location { get; set; }
        public Dates dates { get; set; }

        public string GetGPA()
        {
            var gradeObj = grade;

            if (gradeObj == null)
                return "<i>MISSING</i>";

            var gpa = gradeObj.value;

            if (gpa == null)
                return "<i>MISSING</i>";

            return gpa;
        }

        public string GetDegree()
        {
            var accred = accreditation;

            if (accred == null)
                return "<i>MISSING</i>";

            var degree = accreditation.education;

            if (degree == null)
                return "<i>MISSING</i>";

            return degree;
        }

        public string GetGradDate()
        {
            var date = dates;

            if (date == null)
                return "<i>MISSING</i>";

            var gradDate = date.completionDate;

            if (gradDate == null)
                return "<i>MISSING</i>";

            return gradDate;
        }
    }

    public class Accreditation
    {
        public string education { get; set; }
        public string inputStr { get; set; }
        public string matchStr { get; set; }
        public string educationLevel { get; set; }
    }

    public class Grade
    {
        public string raw { get; set; }
        public string metric { get; set; }
        public string value { get; set; }
    }

    public class Location
    {
        public string formatted { get; set; }
        public object streetNumber { get; set; }
        public object street { get; set; }
        public object apartmentNumber { get; set; }
        public string city { get; set; }
        public object postalCode { get; set; }
        public string state { get; set; }
        public string country { get; set; }
        public string rawInput { get; set; }
    }

    public class Dates
    {
        public string completionDate { get; set; }
        public bool isCurrent { get; set; }
    }

    public class Section
    {
        public string sectionType { get; set; }
        public float[] bbox { get; set; }
        public int pageIndex { get; set; }
        public string text { get; set; }
    }

    public class Workexperience
    {
        public string jobTitle { get; set; }
        public string organization { get; set; }
        public object location { get; set; }
        public Dates1 dates { get; set; }
        public string jobDescription { get; set; }

        public string GetDates()
        {
            var date = dates;

            if (date == null)
                return "<i>MISSING</i>";

            var startDate = date.startDate;

            if (startDate == null)
                startDate = "<i>MISSING</i>";

            var endDate = date.endDate;

            if (endDate == null)
                endDate = "<i>MISSING</i>";

            return $"{startDate} - {endDate}";
        }
    }

    public class Dates1
    {
        public string startDate { get; set; }
        public string endDate { get; set; }
        public int monthsInPosition { get; set; }
        public bool isCurrent { get; set; }
    }

    public class Meta
    {
        public string identifier { get; set; }
        public bool ready { get; set; }
        public bool failed { get; set; }
        public DateTime readyDt { get; set; }
        public object user { get; set; }
        public string fileName { get; set; }
    }

    public class Error
    {
        public object errorCode { get; set; }
        public object errorDetail { get; set; }
    }

    public class UploadRoot
    {
        public string fileName { get; set; }
        public string identifier { get; set; }
    }
}