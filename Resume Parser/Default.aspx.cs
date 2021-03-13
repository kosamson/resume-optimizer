/* 
 * Default.aspx.cs
 * 
 * @contributors: Kolby Samson (@kosamson), Jorge Alvarez (@J-Alv)
 * @purpose: Main backend code for the default main page of the web application
 *           handles the input of Resume files and the output display from calling
 *           the various Azure services and third-party (Affinda and Indeed) API's
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI; // ASP.NET (on App Service)
using System.Web.UI.WebControls; // ASP.NET
using System.Text; // ASP.NET
using Azure.Storage.Blobs; // Blob Storage
using Microsoft.Data.SqlClient; // SQL Client
using System.IO;
using System.Security.Cryptography; // MD5 Hash


namespace Resume_Parser
{
    public partial class _Default : Page
    {
        BlobClient blob;
        ResumeParser resumeparser;

        private static readonly string sqlDBConnString = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            Label1.Font.Size = 15;
            Label1.Font.Bold = true;

            Label2.Font.Size = 15;
            Label2.Font.Bold = true;

            headLabel.Font.Size = 17;
            headLabel.Font.Bold = true;

            ContactInfoTable.CellPadding = 10;
            EducationTable.CellPadding = 10;
            ExperienceTable.CellPadding = 10;
            SkillsTable.CellPadding = 10;
        }

        // Action Listener for the "Parse" button on the main page
        // Takes in the input uploaded file and the text from the 
        // position and location text boxes and outputs a display
        // of the service through the resume parser and job finder
        protected void runButton_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();

            blob = new BlobClient(
                "",
                "stringparser",
                "input.pdf");

            mainLabel.Text = "This might take a while";
            mainLabel.Visible = true;

            if (Uploader.HasFile)
            {
                try
                {   
                    
                    string fileName = Uploader.FileName;
                    byte[] fileContent = Uploader.FileBytes;

                    //use file content to make a hash value
                    string hash = makeHash(fileContent);

                    //check the db to see if the hash value exists
                    bool fileExists = checkHashValue(hash);

                    //if the file doesnt exist on the db, upload to the blob container and do a full resume parse
                    if(fileExists == false)
                    {
                        using (var stream = new MemoryStream(fileContent, writable: false))
                        {
                            blob.Upload(stream, true);
                        }
                        
                        resumeparser = new ResumeParser(fileName);
                        addHashToDb(hash, resumeparser.fileID);
                    }

                    //else if the file does exist, dont upload to the blob and call on secondary resumeParser constructor
                    else if(fileExists == true)
                    {
                        string affID = getAffindaID(hash);
                        resumeparser = new ResumeParser(fileName, affID);
                    }

                    mainLabel.Text = $"Successfully parsed file: {fileName}";

                    ParserSectionLabel.Font.Size = 30;
                    ParserSectionLabel.Visible = true;
                    ParserSectionLabel.Font.Bold = true;
                    
                    // Build tables displaying the output
                    // of the Affinda Resume Parser JSON
                    // in distinct sections
                    BuildParserDisplay();

                    // Execute extra resume parser and
                    // job finder functionality if the
                    // position for the resume is specified
                    if (positionText.Text != "") {
                        CompareHeaders();

                        if (!fileExists)
                        {
                            StoreResumeHeaders();
                        }

                        FindJobs();
                    }
                }

                catch (Exception ex)
                {
                    sb.Append("<br/> Error <br/>");
                    sb.AppendFormat("Unable to save file <br/> {0}", ex.Message);
                    mainLabel.Text = sb.ToString();
                    mainLabel.Visible = true;
                }
            }

            else
            {
                mainLabel.Text = "No file uploaded";
                mainLabel.Visible = true;
            }
        }

        // Updates SQL database with the hash digest and
        // Affinda File ID of a newly uploaded file to
        // save on API Calls in the future (if the same
        // file is uploaded again)
        protected void addHashToDb(string hash, string fileID)
        {
            using (SqlConnection conn = new SqlConnection(sqlDBConnString))
            {
                conn.Open();

                // Add new RESUME_FILE object with hash digest and Affinda File ID
                string cmdStr = $"INSERT INTO RESUME_FILE VALUES('{hash}', '{fileID}')";
                SqlCommand command = new SqlCommand(cmdStr, conn);

                command.ExecuteNonQuery();
                conn.Close();
            }
        }

        // Retrieves the associated Affinda File ID
        // of an uploaded file with the corresponding MD5 Hash Digest
        // from the RESUME_FILE entity in the SQL database
        protected string getAffindaID(string hash)
        {
            using (SqlConnection conn = new SqlConnection(sqlDBConnString))
            {
                conn.Open();

                // Select the Affinda ID of the file with the corresponding hash digest
                string cmdStr = $"SELECT affinda_id FROM RESUME_FILE WHERE hashvalue = '{hash}';";

                SqlCommand command = new SqlCommand(cmdStr, conn);

                SqlDataReader reader = command.ExecuteReader();
                reader.Read();

                string retVal = reader.GetString(0);

                conn.Close();

                return retVal;
            }
        }

        // Utility function to check if a file has already been uploaded
        // and parsed by the Affinda API (through our service) by querying
        // our SQL database using an uploaded file's MD5 hash digest as a key
        protected bool checkHashValue(string hash)
        {
            using (SqlConnection conn = new SqlConnection(sqlDBConnString))
            {
                conn.Open();
                string cmdStr = $"SELECT hashvalue FROM RESUME_FILE WHERE hashvalue = '{hash}';";

                SqlCommand command = new SqlCommand(cmdStr, conn);

                SqlDataReader reader = command.ExecuteReader();

                // Row containing hash is found, file has already been uploaded
                if(reader.HasRows)
                {

                    conn.Close();
                    return true;
                }

                // Row containing hash is no found, file has not been uploaded
                else
                {

                    conn.Close();
                    return false;
                }

            }

        }

        // Utility function to calculate the MD5 hash digest
        // of an input file using its binary byte content
        protected string makeHash(byte[] fileContent)
        {

            MD5 hash = MD5.Create();
            byte[] hashBytes = hash.ComputeHash(fileContent);

            StringBuilder hashBuilder = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                hashBuilder.Append(hashBytes[i].ToString("X2"));
            }

            return hashBuilder.ToString();
        }

        // High-level function to execute the 
        // building of displays for the resume
        // parser
        protected void BuildParserDisplay()
        {
            BuildContactInfoTable();
            BuildEducationTable();
            BuildExperienceTable();
            BuildSkillsTable();
            BuildSectionsTable();
        }

        // Display utility function to create a table row
        // out of a list of column labels for use in a 
        // resume parser output table
        protected TableRow BuildLabelsRow(List<string> labels)
        {
            var row = new TableRow();

            // Add bolded text cells to the table row
            foreach (string label in labels)
            {
                row.Cells.Add(new TableCell { Text = $"<b>{label}</b>" });
            }

            return row;
        }

        // Display utility function to create a table row
        // out of a list of string values for use in a 
        // resume parser output table
        protected TableRow BuildValuesRow(List<string> values)
        {
            var row = new TableRow();
            TableCell c = new TableCell();

            foreach (string value in values)
            {
                row.Cells.Add(new TableCell { Text = $"{value}" });
            }

            return row;
        }

        // Display utility function to build a graphic table of labels and values
        // with an arbitrary amount of rows and columns
        protected void buildTable(Table table, Label tableLabel, string labelTitle, List<string> labels, List<List<string>> values)
        {
            tableLabel.Text = $"<b>{labelTitle}</b>";
            tableLabel.Visible = true;
            table.GridLines = GridLines.Both;

            // Build labels row
            table.Rows.Add(BuildLabelsRow(labels));
            
            // Build all value rows
            foreach (List<string> rowValues in values)
            {
                table.Rows.Add(BuildValuesRow(rowValues));
            }
        }

        // Utility function to transform a list of string values
        // into a vertical list (using the aspx newline format)
        // through a formatted string
        protected string BuildVerticalList(List<string> items)
        {
            if (items == null || items.Count == 0)
                return "<i>MISSING</i>";

            return String.Join("<br/>", items);
        }

        // Same as BuildVerticalList(List<string>) but for string arrays
        protected string BuildVerticalList(string[] items)
        {
            if (items == null || items.Length == 0)
                return "<i>MISSING</i>";

            return String.Join("<br/>", items);
        }

        // Subfunction to create the Contact Info table for the
        // resume parser output
        protected void BuildContactInfoTable()
        {
            var labels = new List<string>() { "Name", "Phone Numbers", "Emails", "Links" };
            var values = new List<List<string>>();

            var contactInfo = new List<string>() {
                                                    resumeparser.GetName(),
                                                    BuildVerticalList(resumeparser.GetPhones()),
                                                    BuildVerticalList(resumeparser.GetEmails()),
                                                    BuildVerticalList(resumeparser.GetLinks())
                                                 };

            values.Add(contactInfo);

            buildTable(ContactInfoTable, ContactInfoLabel, "Contact Info", labels, values);
        }

        // Subfunction to create the Education table for the
        // resume parser output
        protected void BuildEducationTable()
        {
            var labels = new List<string>() { "School Name", "Degree", "GPA", "Graduation Date" };
            var values = new List<List<string>>();

            Education[] educationHistory = resumeparser.GetEducationHistory();

            // Error Case: No education sections found in resume
            if (educationHistory == null || educationHistory.Length == 0)
            {
                values.Add(new List<string> { "<i>MISSING</i>", "<i>MISSING</i>", "<i>MISSING</i>", "<i>MISSING</i>" });
            }

            else
            {
                foreach (Education education in educationHistory)
                {
                    var educationInfo = new List<string>
                    {
                        education.organization == null || education.organization == "" ? "<i>MISSING</i>" : education.organization,
                        education.GetDegree(),
                        education.GetGPA(),
                        education.GetGradDate()
                    };

                    values.Add(educationInfo);
                }
            }

            buildTable(EducationTable, EducationLabel, "Education", labels, values);
        }

        // Subfunction to create the Experience table for the
        // resume parser output
        protected void BuildExperienceTable()
        {
            var labels = new List<string>() { "Job Title", "Company", "Dates", "Job Description" };
            var values = new List<List<string>>();

            Workexperience[] workexperiences = resumeparser.GetWorkExperience();

            // Error Case: No experiences found in resume
            if (workexperiences == null || workexperiences.Length == 0)
            {
                values.Add(new List<string> { "<i>MISSING</i>", "<i>MISSING</i>", "<i>MISSING</i>", "<i>MISSING</i>" });
            }

            else
            {
                foreach (Workexperience experience in workexperiences)
                {
                    var experienceInfo = new List<string>
                    {
                        experience.jobTitle == null || experience.jobTitle == "" ? "<i>MISSING</i>" : experience.jobTitle,
                        experience.organization == null || experience.organization == "" ? "<i>MISSING</i>" : experience.organization,
                        experience.GetDates(),
                        experience.jobDescription == null || experience.jobDescription == "" ? "<i>MISSING</i>" : experience.jobDescription
                    };

                    values.Add(experienceInfo);
                }
            }

            buildTable(ExperienceTable, ExperienceLabel, "Experience", labels, values);
        }

        // Subfunction to create the Skills table for the
        // resume parser output
        protected void BuildSkillsTable()
        {
            string[] skillArr = resumeparser.GetSkills();

            List<string> skills = null;

            if (skillArr == null)
                skills = new List<string>() { " <i>MISSING</i> " };

            else
                skills = skillArr.ToList();

            string skillStr = BuildVerticalList(skills);

            var row = new List<string> { skillStr };

            SkillsLabel.Text = $"<b>Skills</b>";
            SkillsLabel.Visible = true;
            SkillsTable.GridLines = GridLines.Both;

            SkillsTable.Rows.Add(BuildValuesRow(row));
        }

        // Subfunction to create the Sections table for the
        // resume parser output
        protected void BuildSectionsTable()
        {
            List<string> sectionNames = resumeparser.GetSectionNames();

            string skillStr = BuildVerticalList(sectionNames);

            var row = new List<string> { skillStr };

            SectionsLabel.Text = $"<b>Sections</b>";
            SectionsLabel.Visible = true;
            SectionsTable.GridLines = GridLines.Both;

            SectionsTable.Rows.Add(BuildValuesRow(row));
        }

        // SQL DB Utility function to retrieve the titles of 
        // all resume sections and insert them into
        // the SQL DB to keep track of common resume sections
        // for specific input careers
        protected void StoreResumeHeaders()
        {
            List<string> sectionHeaders = resumeparser.GetSectionNames();
            string jobTitle = positionText.Text;

            using (SqlConnection conn = new SqlConnection(sqlDBConnString))
            {
                conn.Open();

                foreach (string sectionHeader in sectionHeaders)
                {
                    // Check if the header already exists for the input job title in the DB,
                    // if it does exist, increment its frequency by one,
                    // else, insert it as a new header with a frequency of one
                    string cmdStr = $"IF EXISTS (SELECT 1 FROM JOB_HEADER WHERE jobName = '{jobTitle}' AND header = '{sectionHeader}') " +
                                         $"BEGIN UPDATE JOB_HEADER SET frequency = frequency + 1 WHERE jobName = '{jobTitle}' AND header = '{sectionHeader}' END " +
                                         $"ELSE BEGIN INSERT INTO JOB_HEADER VALUES ('{jobTitle}', '{sectionHeader}', 1) END;";
                    
                    SqlCommand command = new SqlCommand(cmdStr, conn);

                    command.ExecuteNonQuery();
                }

                conn.Close();
            }
        }

        // Resume Parser functionality to check the current uploaded resume
        // against common resume sections for the input career to see if 
        // they are missing any important sections
        protected void CompareHeaders()
        {
            string jobTitle = positionText.Text;
            List<string> sectionHeaders = resumeparser.GetSectionNames();

            ResumeChanges.Text = $"Common {jobTitle} Resume Sections";

            using (SqlConnection conn = new SqlConnection(sqlDBConnString))
            {
                conn.Open();

                // Get top 10 most common section headers from parsed resumes
                SqlCommand selectall = new SqlCommand($"SELECT TOP 10 header, frequency FROM JOB_HEADER WHERE jobName = '{jobTitle}' ORDER BY frequency DESC;", conn);

                string result = "<br/>";

                var reader = selectall.ExecuteReader();

                while (reader.Read())
                {
                    var sectionName = reader[0];
                    var frequency = reader[1];

                    // Resume contains common header, mark green
                    if (sectionHeaders.Contains(sectionName))
                    {
                        result += $"<span style=\"color: #008000\"><b>{sectionName}</b></span>(frequency: {frequency})<br/>";
                    }

                    // Resume does not contain common header, mark red
                    else
                    {
                        result += $"<span style=\"color: #FF0000\"><b>{sectionName}</b></span>,(frequency: {frequency})<br/>";
                    }
                }

                // Error Case: No data on the input career is found
                if (result == "<br/>")
                {
                    result = $"<br/>No resume section data found for {jobTitle}. You might be the first!";
                }

                ChangeList.Text = result;

                conn.Close();
            }

            ResumeChanges.Font.Size = 30;
            ResumeChanges.Font.Bold = true;

            ResumeChanges.Visible = true;
            ChangeList.Visible = true;
        }

        // Service functionality integrated with the Indeed API (indirectly
        // through a Web Crawler in IndeedCrawler.cs) to find jobs related
        // to the input career name
        protected void FindJobs()
        {
            //save the job position the user is looking for
            string jobTitle = positionText.Text;

            JobLabel.Font.Size = 30;
            JobLabel.Font.Bold = true;

            //dictionary that will be populated with the url's and job titles of the indeed
            //website search. Uses a webcrawler to emulate API endpoints when searching
            Dictionary<string, string> jobList = new Dictionary<string, string>();
            IndeedCrawler crawler = new IndeedCrawler();

            //calls on crawl() which crawls indeed's search query for the job position
            crawler.crawl(positionText.Text, locText.Text, ref jobList);

            //reset the jobList that will be displayed
            JobList.Text = "";

            //if no jobs could be found with that title then show no jobs found and return
            if(jobList.Count == 0)
            {
                noList.Text = $"No Job Listings for {jobTitle} Found";
                noList.Visible = true;
                JobLabel.Visible = true;
                return;
            }
            
            //loop through all the jobs in the dictionary and display them to the user
            int i = 1;
            foreach (KeyValuePair<string, string> job in jobList)
            {
                JobList.Text += i + ": <a target=\"_blank\"href=" + job.Value + ">" + job.Key  + "</a><br/>";
                i++;
            }

            //Display label for section
            JobLabel.Text = $"{jobTitle} Job Listings<br/>";

            noList.Visible = false;
            JobList.Visible = true;
            JobLabel.Visible = true;
        }
    }
}