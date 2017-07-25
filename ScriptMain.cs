#region Help:  Introduction to the script task
/* The Script Task allows you to perform virtually any operation that can be accomplished in
 * a .Net application within the context of an Integration Services control flow. 
 * 
 * Expand the other regions which have "Help" prefixes for examples of specific ways to use
 * Integration Services features within this script task. */
#endregion


#region Namespaces
using System;
using System.Data;
using Microsoft.SqlServer.Dts.Runtime;
using System.Windows.Forms;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
#endregion

namespace ST_fb3da1e60d164d10a3eec78f72c1a897
{
    /// <summary>
    /// ScriptMain is the entry point class of the script.  Do not change the name, attributes,
    /// or parent of this class.
    /// </summary>
	[Microsoft.SqlServer.Dts.Tasks.ScriptTask.SSISScriptTaskEntryPointAttribute]

    
    public partial class ScriptMain : Microsoft.SqlServer.Dts.Tasks.ScriptTask.VSTARTScriptObjectModelBase
	{
        //
        class AzureTokenGenerator
        {
            static String HUB_NAME_DEV = "DEV";
            static String CONNECTION_STRING_DEV = "";
            static String HUB_NAME_QA = "QA";
            static String CONNECTION_STRING_QA = "";
            static String HUB_NAME_STG = "STAGING";
            static String CONNECTION_STRING_STG = "";
            
            //set the token expiry
            public static string GetExpiry()
            {
                TimeSpan sinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1);
                return Convert.ToString((int)sinceEpoch.TotalSeconds + 3600);
            }

            private string url;
            private string enviromentType;

            public string endpoint { get; private set; }
            public string keyName { get; private set; }
            public string keyValue { get; private set; }
            public string generatedToken { get; private set; }
            public string resourceUri { get; private set; }

            // parse connection string to obtain key name and value.
            public void ConnectionStringUtility(string connectionString)
            {

                char[] separator = { ';' };
                string[] parts = connectionString.Split(separator);
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].StartsWith("Endpoint"))
                        endpoint = "https" + parts[i].Substring(11);
                    if (parts[i].StartsWith("SharedAccessKeyName"))
                        keyName = parts[i].Substring(20);
                    if (parts[i].StartsWith("SharedAccessKey"))
                        keyValue = parts[i].Substring(16);
                }
            }

            // generate the Azure token.
            public void setSASToken()
            {
                TimeSpan sinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1);
                var week = 60 * 60 * 24 * 7;
                var expiry = Convert.ToString((int)sinceEpoch.TotalSeconds + week);
                string stringToSign = HttpUtility.UrlEncode(resourceUri) + "\n" + expiry;
                HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(keyValue));
                var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
                var sasToken = String.Format(CultureInfo.InvariantCulture, "SharedAccessSignature sr={0}&sig={1}&se={2}&skn={3}", HttpUtility.UrlEncode(resourceUri), HttpUtility.UrlEncode(signature), expiry, keyName);
                this.generatedToken = sasToken;
            }

            public void runGenerator(string connection)
            {
                this.ConnectionStringUtility(connection);
                this.setSASToken();
            }

            public AzureTokenGenerator(string url, string enviromentType)
            {
                this.resourceUri = url;

                switch (enviromentType)
                {
                    case "DEV":
                        this.runGenerator(CONNECTION_STRING_DEV);
                        break;
                    case "QA":
                        this.runGenerator(CONNECTION_STRING_QA);
                        break;
                    case "STAGING":
                        this.runGenerator(CONNECTION_STRING_STG);
                        break;
                    default:
                        this.runGenerator(CONNECTION_STRING_DEV);
                        break;
                }
            }

        }
        //  
        #region Help:  Using Integration Services variables and parameters in a script
        /* To use a variable in this script, first ensure that the variable has been added to 
         * either the list contained in the ReadOnlyVariables property or the list contained in 
         * the ReadWriteVariables property of this script task, according to whether or not your
         * code needs to write to the variable.  To add the variable, save this script, close this instance of
         * Visual Studio, and update the ReadOnlyVariables and 
         * ReadWriteVariables properties in the Script Transformation Editor window.
         * To use a parameter in this script, follow the same steps. Parameters are always read-only.
         * 
         * Example of reading from a variable:
         *  DateTime startTime = (DateTime) Dts.Variables["System::StartTime"].Value;
         * 
         * Example of writing to a variable:
         *  Dts.Variables["User::myStringVariable"].Value = "new value";
         * 
         * Example of reading from a package parameter:
         *  int batchId = (int) Dts.Variables["$Package::batchId"].Value;
         *  
         * Example of reading from a project parameter:
         *  int batchId = (int) Dts.Variables["$Project::batchId"].Value;
         * 
         * Example of reading from a sensitive project parameter:
         *  int batchId = (int) Dts.Variables["$Project::batchId"].GetSensitiveValue();
         * */

        #endregion

        #region Help:  Firing Integration Services events from a script
        /* This script task can fire events for logging purposes.
         * 
         * Example of firing an error event:
         *  Dts.Events.FireError(18, "Process Values", "Bad value", "", 0);
         * 
         * Example of firing an information event:
         *  Dts.Events.FireInformation(3, "Process Values", "Processing has started", "", 0, ref fireAgain)
         * 
         * Example of firing a warning event:
         *  Dts.Events.FireWarning(14, "Process Values", "No values received for input", "", 0);
         * */
        #endregion

        #region Help:  Using Integration Services connection managers in a script
        /* Some types of connection managers can be used in this script task.  See the topic 
         * "Working with Connection Managers Programatically" for details.
         * 
         * Example of using an ADO.Net connection manager:
         *  object rawConnection = Dts.Connections["Sales DB"].AcquireConnection(Dts.Transaction);
         *  SqlConnection myADONETConnection = (SqlConnection)rawConnection;
         *  //Use the connection in some code here, then release the connection
         *  Dts.Connections["Sales DB"].ReleaseConnection(rawConnection);
         *
         * Example of using a File connection manager
         *  object rawConnection = Dts.Connections["Prices.zip"].AcquireConnection(Dts.Transaction);
         *  string filePath = (string)rawConnection;
         *  //Use the connection in some code here, then release the connection
         *  Dts.Connections["Prices.zip"].ReleaseConnection(rawConnection);
         * */
        #endregion


        /// <summary>
        /// This method is called when this script task executes in the control flow.
        /// Before returning from this method, set the value of Dts.TaskResult to indicate success or failure.
        /// To open Help, press F1.
        /// </summary>
        public void Main()
		{
       
            //resources:
            //https://msdn.microsoft.com/en-us/library/azure/dn495627.aspx
            //https://docs.microsoft.com/en-us/rest/api/eventhub/generate-sas-token

            String url = (String)Dts.Variables["User::url"].Value;
            String environmentType = (String)Dts.Variables["User::environment"].Value;

            String sasToken = new AzureTokenGenerator(url, environmentType).generatedToken;
            Dts.Variables["User::token"].Value = sasToken;

            MessageBox.Show(sasToken);

            Boolean x = true;
            Dts.Events.FireInformation(1, sasToken, sasToken, "", 1, ref x);


            Dts.TaskResult = (int)ScriptResults.Success;
        }

        #region ScriptResults declaration
        /// <summary>
        /// This enum provides a convenient shorthand within the scope of this class for setting the
        /// result of the script.
        /// 
        /// This code was generated automatically.
        /// </summary>
        enum ScriptResults
        {
            Success = Microsoft.SqlServer.Dts.Runtime.DTSExecResult.Success,
            Failure = Microsoft.SqlServer.Dts.Runtime.DTSExecResult.Failure
        };
        #endregion

	}
}