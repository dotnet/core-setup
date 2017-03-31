// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Build.Framework;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Build.Construction;
using System.Net.Http;
using System.Xml;
using System.Globalization;
using System.Threading.Tasks;
using System.Linq;

namespace Microsoft.DotNet.Build.Tasks
{
    public partial class GetAzureBlobList : BuildTask
    {
        /// <summary>
        /// Azure Storage account connection string.  Supersedes Account Key / Name.  
        /// Will cause errors if both are set.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// The Azure account key used when creating the connection string.
        /// When we fully deprecate these, can just make them get; only.
        /// </summary>
        public string AccountKey { get; set; }

        /// <summary>
        /// The Azure account name used when creating the connection string.
        /// When we fully deprecate these, can just make them get; only.
        /// </summary>
        public string AccountName { get; set; }


        /// <summary>
        /// The name of the container to access.  The specified name must be in the correct format, see the
        /// following page for more info.  https://msdn.microsoft.com/en-us/library/azure/dd135715.aspx
        /// </summary>
        [Required]
        public string ContainerName { get; set; }

        [Output]
        public string[] BlobNames { get; set; }

        public string FilterBlobNames { get; set; }

        public override bool Execute()
        {
            return ExecuteAsync().GetAwaiter().GetResult();
        }

        // This code is duplicated in BuildTools task DownloadFromAzure, and that code should be refactored to permit blob listing.
        public async Task<bool> ExecuteAsync()
        {
            if (!string.IsNullOrEmpty(ConnectionString))
            {
                if (!(string.IsNullOrEmpty(AccountKey) && string.IsNullOrEmpty(AccountName)))
                {
                    Log.LogError("If the ConnectionString property is set, you must not provide AccountKey / AccountName.  These values will be deprecated in the future.");
                }
                else
                {
                    Tuple<string, string> parsedValues = AzureHelper.ParseConnectionString(ConnectionString);
                    if (parsedValues == null)
                    {
                        Log.LogError("Error parsing connection string.  Please review its value.");
                    }
                    else
                    {
                        AccountName = parsedValues.Item1;
                        AccountKey = parsedValues.Item2;
                    }
                }
            }
            else if (string.IsNullOrEmpty(AccountKey) || string.IsNullOrEmpty(AccountName))
            {
                Log.LogError("Error, must provide either ConnectionString or AccountName with AccountKey");
            }

            if (Log.HasLoggedErrors)
            {
                return false;
            }

            List<string> blobsNames = new List<string>();
            string urlListBlobs = string.Format("https://{0}.blob.core.windows.net/{1}?restype=container&comp=list", AccountName, ContainerName);

            Log.LogMessage(MessageImportance.Low, "Sending request to list blobsNames for container '{0}'.", ContainerName);

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var createRequest = AzureHelper.RequestMessage("GET", urlListBlobs, AccountName, AccountKey);

                    XmlDocument responseFile;
                    string nextMarker = string.Empty;
                    using (HttpResponseMessage response = await AzureHelper.RequestWithRetry(Log, client, createRequest))
                    {
                        responseFile = new XmlDocument();
                        responseFile.LoadXml(await response.Content.ReadAsStringAsync());
                        XmlNodeList elemList = responseFile.GetElementsByTagName("Name");

                        blobsNames.AddRange(elemList.Cast<XmlNode>()
                                                    .Select(x => x.InnerText)
                                                    .ToList());

                        nextMarker = responseFile.GetElementsByTagName("NextMarker").Cast<XmlNode>().FirstOrDefault()?.InnerText;
                    }
                    while (!string.IsNullOrEmpty(nextMarker))
                    {
                        urlListBlobs = string.Format($"https://{AccountName}.blob.core.windows.net/{ContainerName}?restype=container&comp=list&marker={nextMarker}");
                        using (HttpResponseMessage response = AzureHelper.RequestWithRetry(Log, client, createRequest).GetAwaiter().GetResult())
                        {
                            responseFile = new XmlDocument();
                            responseFile.LoadXml(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                            XmlNodeList elemList = responseFile.GetElementsByTagName("Name");

                            blobsNames.AddRange(elemList.Cast<XmlNode>()
                                                        .Select(x => x.InnerText)
                                                        .ToList());

                            nextMarker = responseFile.GetElementsByTagName("NextMarker").Cast<XmlNode>().FirstOrDefault()?.InnerText;
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(FilterBlobNames))
                    {
                        BlobNames = blobsNames.Where(b => b.StartsWith(FilterBlobNames)).ToArray();
                    }
                    else
                    {
                        BlobNames = blobsNames.ToArray();
                    }
                }
                catch (Exception e)
                {
                    Log.LogErrorFromException(e, true);
                }
                return !Log.HasLoggedErrors;
            }
        }
    }

}
