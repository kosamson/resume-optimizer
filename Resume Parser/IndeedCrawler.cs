/* 
 * ResumeParser.cs
 * 
 * @contributors: Kolby Samson (@kosamson), Jorge Alvarez (@J-Alv)
 * @purpose: Wrapper class containing functionality for crawling
 *           the indeed website by creating unique URL's that emulate
 *           an API call. It searches for the first 10 job postings related
 *           to the job position the user gives.
 */
using System;
using System.Net;
using System.Net.Http;
using HtmlAgilityPack;
using System.Collections.Generic;

namespace Resume_Parser
{
    class IndeedCrawler
    {
        private HttpClient client;

        //initialize the client and the base address to indeed.com
        public IndeedCrawler()
        {
            client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
            client.BaseAddress = new Uri("https://www.indeed.com/");
        }

        //function that generates the URL using the job position given and populates the dictionary passed through
        //with the top 10 <a href> values that are recognized as job postings on indeed.
        public void crawl(string position, string location, ref Dictionary<string, string> jobList)
        {
            //allows client to connect to any server protocol
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            //bools to see if the user wants to look for jobs within a specified location
            bool addPosition = true;
            bool addLocation = true;

            position = position.Trim(' ');
            location = location.Trim(' ');

            //if position is empty, don't look for jobs
            if (position.Equals(""))
            {
                addPosition = false;
            }

            //if location is empty, dont specify the location
            if (position.Equals(""))
            {
                addLocation = false;
            }

            //if both are empty, return
            if (addPosition == false && addLocation == false)
            {
                return;
            }

            //split the position and locations so that they can be formatted into the URL
            string[] positionName = position.Split(' ');
            string[] locationName = location.Split(',', ' ');

            string url = "jobs?";

            //if they are looking for a job add the jobs
            if (addPosition == true)
            {
                //specify this is a query
                url += "q=";

                //add each word
                foreach (var word in positionName)
                {
                    url += word + "+";
                }
            }

            //if they are looking for a location add the location
            if (addLocation == true)
            {
                //start with specifiying this is location
                url += "&l=";

                //if the location passed has multuple lines 
                if (locationName.Length > 1)
                {
                    //loop through the given words
                    int index = 0;
                    while (index < locationName.Length)
                    {
                        //if its the first, part of city name
                        if (index == 0)
                        {
                            url += locationName[0];

                        }

                        //if not first, check if its empty
                        else if (locationName[index].Equals("") == false)
                        {
                            //if not empty and the value is greater than 2, part of the city
                            if(locationName[index].Length > 2)
                            {
                                url += "+" + locationName[index];
                            }
                            //if length is <= 2 then its the state
                            else
                            {
                                url += "%2C+" + locationName[index];
                            }

                        }

                        index++;
                    }
                }
                //if only one name is given its the city
                else
                {
                    url += locationName[0];
                }
            }

            //only look for recent postings
            url += "&fromage=3";

            //get the html from the website
            HttpResponseMessage response = client.GetAsync(url).Result;

            //check if the status code was 400 level, return fail code
            if ((int)response.StatusCode > 399 && (int)response.StatusCode < 499 || (int)response.StatusCode == 300)
            {
                return;
            }

            //check if the status code was 500 level
            if ((int)response.StatusCode > 499 && (int)response.StatusCode < 599)
            {
                //retry 3 times 
                int retries = 0;

                while (retries < 3 && (int)response.StatusCode > 499 && (int)response.StatusCode < 599)
                {
                    response = client.GetAsync(url).Result;
                    retries++;
                }

                //if max amount of retries have happened, return
                if (retries >= 3)
                {
                    return;
                }
            }

            //get the result into a string if it worked properly
            string result = response.Content.ReadAsStringAsync().Result;

            //create htmldoc and load the return html from the response
            HtmlWeb hw = new HtmlWeb();
            HtmlDocument doc = hw.Load("https://www.indeed.com/" + url);

            //check to see if there are nodes for href in the html return
            var isEmpty = doc.DocumentNode.SelectNodes("//a[@href]");
            int jobLimit = 0;

            //list to keep track of seen urls
            List<string> seen = new List<string>();

            //checks to see if there are no href nodes in the html return, if there are none just exit
            if (isEmpty != null)
            {
                //check each link node in the document
                foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]"))
                {

                    //attribute to get the href
                    HtmlAttribute att = link.Attributes["href"];

                    //title of the job position
                    string title = link.InnerText.Trim();

                    //check to see if /pageread/ or /rc/ are in the href, if they are then it is a job posting
                    int loc = att.Value.IndexOf("/pagead/");
                    int otherloc = att.Value.IndexOf("/rc/");

                    //if either exist
                    if (loc > -1 || otherloc > -1)
                    {
                        //check to see if it has been seen before, if not add it to the dictionary to return
                        if (seen.Contains(title) == false)
                        {
                            seen.Add(title);
                            jobList.Add(title, "https://www.indeed.com" + att.Value);
                            jobLimit++;
                        }
                    }

                    //if we've reached the job limit, return
                    if (jobLimit == 10)
                    {
                        return;
                    }
                }
            }
        }
    }
}


