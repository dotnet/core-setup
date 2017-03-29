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
using System.Diagnostics;
using System.Threading;

namespace Microsoft.DotNet.Build.Tasks
{
    public partial class FinalizeBuild : BuildTask
    {
        [Required]
        public string SemaphoreBlob { get; set; }
        [Required]
        public string FinalizeContainer { get; set; }
        public string MaxWait { get; set; }
        public string Delay { get; set; }

        public string AccountName { get; set; }
        public string AccountKey { get; set; }
        public string ConnectionString { get; set; }
        [Required]
        public string ContainerName { get; set; }
        [Required]
        public string Channel { get; set; }
        [Required]
        public string CommitHash { get; set; }

        private const int s_MaxWaitDefault = 120; // seconds
        private const int s_DelayDefault = 500; // milliseconds
        private CancellationTokenSource _cancellationTokenSource;
        private Task _leaseRenewalTask;
        private TimeSpan _maxWait;
        private TimeSpan _delay;
        private string _leaseUrl;

        public override bool Execute()
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


            Console.WriteLine("Attach debugger now <press ENTER to continue>");
            Console.ReadLine();
            _leaseUrl = GetBlobLeaseRequestUrl(AccountName, ContainerName, SemaphoreBlob);
            _maxWait = !string.IsNullOrWhiteSpace(MaxWait) ? TimeSpan.Parse(MaxWait) : TimeSpan.FromSeconds(s_MaxWaitDefault);
            _delay = !string.IsNullOrWhiteSpace(Delay) ? TimeSpan.Parse(Delay) : TimeSpan.FromMilliseconds(s_DelayDefault);

            if(!FinalizeContainer.EndsWith("/"))
            {
                FinalizeContainer = $"{FinalizeContainer}/";
            }
            string targetVersionFile = $"{FinalizeContainer}{CommitHash}";

            CreateBlobIfNotExists(SemaphoreBlob);

            string leaseId = AcquireLeaseOnBlob(SemaphoreBlob);

            if(IsLatestSpecifiedVersion(targetVersionFile))
            {
                ReleaseLeaseOnBlob(SemaphoreBlob, leaseId);
                return true;
            }
            else
            {
                Regex versionFileRegex = new Regex(@"(?<CommitHash>[\w\d]}40})");

                GetBlobList(FinalizeContainer)
                    .Select(s => s.Replace("/dotnet/", ""))
                    .Where(w => versionFileRegex.IsMatch(w))
                    .ToList()
                    .ForEach(f => TryDeleteBlob(f));

                CreateBlobIfNotExists(targetVersionFile);

            }
            return !Log.HasLoggedErrors;
        }

        public string [] GetBlobList(string path)
        {
            GetAzureBlobList azureBlobList = new GetAzureBlobList();
            azureBlobList.AccountName = AccountName;
            azureBlobList.AccountKey = AccountKey;
            azureBlobList.ConnectionString = ConnectionString;
            azureBlobList.ContainerName = ContainerName;
            azureBlobList.FilterBlobNames = path;
            azureBlobList.BuildEngine = this.BuildEngine;
            azureBlobList.HostObject = this.HostObject;
            azureBlobList.Execute();
            
            return azureBlobList.BlobNames;
        }

        public void CopyBlobs(string sourceFolder, string destinationFolder)
        {
            foreach(string blob in GetBlobList(sourceFolder))
            {
                string source = blob.Replace("/dotnet/", "");
                string directoryName = Path.GetDirectoryName(blob);
                string targetName = Path.GetFileName(blob)
                    .Replace(directoryName, "chcosta_latest");
                string target = $"{destinationFolder}{targetName}";
                /* Actually copy the blob */
            }
        }
        public bool TryDeleteBlob(string path)
        {
            string deleteUrl = $"{GetBlobRootRequestUrl(AccountName, ContainerName, path)}";
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    Tuple<string, string> snapshots = new Tuple<string, string>("x-ms-lease-delete-snapshots", "include");
                    List<Tuple<string, string>> additionalHeaders = new List<Tuple<string, string>>() { snapshots };
                    var request = AzureHelper.RequestMessage("DELETE", deleteUrl, AccountName, AccountKey, additionalHeaders);
                    using (HttpResponseMessage response = AzureHelper.RequestWithRetry(Log, client, request).GetAwaiter().GetResult())
                    {
                        return response.IsSuccessStatusCode;
                    }
                }
                catch (Exception e)
                {
                    Log.LogErrorFromException(e, true);
                }
            }

            return false;
        }
        public void CreateBlobIfNotExists(string path)
        {
            var blobList = GetBlobList(path);
            if(blobList.Count() == 0)
            {
                string semaphoreFile = Path.GetFileName(path);
                using (StreamWriter writer = File.CreateText(semaphoreFile))
                {
                    writer.WriteLine(DateTime.Now.ToString());
                }
                Dictionary<string, string> metadata = new Dictionary<string, string>();
                metadata.Add("RelativeBlobPath", $"{path}");
                Microsoft.Build.Utilities.TaskItem item = new Microsoft.Build.Utilities.TaskItem(semaphoreFile, metadata);
                UploadToAzure uploadToAzure = new UploadToAzure();
                uploadToAzure.AccountName = AccountName;
                uploadToAzure.AccountKey = AccountKey;
                uploadToAzure.BuildEngine = BuildEngine;
                uploadToAzure.ConnectionString = ConnectionString;
                uploadToAzure.ContainerName = ContainerName;
                uploadToAzure.Items = new ITaskItem[] { item };
                uploadToAzure.Execute();
            }

        }

        public bool IsLatestSpecifiedVersion(string versionFile)
        {

            var blobList = GetBlobList(versionFile);
            return blobList.Count() != 0;
        }
        public string AcquireLeaseOnBlob(
            string blob)
        {

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            while (stopWatch.ElapsedMilliseconds < _maxWait.TotalMilliseconds)
            {
                try
                {
                    string leaseId = AcquireLeaseOnBlobAsync().GetAwaiter().GetResult();
                    _cancellationTokenSource = new CancellationTokenSource();
                    _leaseRenewalTask = Task.Run(() =>
                    { AutoRenewLeaseOnBlob(this, AccountName, AccountKey, ContainerName, blob, leaseId, Log); },
                      _cancellationTokenSource.Token);
                    return leaseId;
                }
                catch (Exception e)
                {
                    Log.LogMessage($"Retrying lease acquisition on {SemaphoreBlob}, {e.Message}");
                    Thread.Sleep(_delay);
                }
            }
            ResetLeaseRenewalTaskState();
            throw new Exception($"Unable to acquire lease on {blob}");

        }

        public async Task<string> AcquireLeaseOnBlobAsync()
        {
            Log.LogMessage(MessageImportance.Low, $"Requesting lease for container/blob '{ContainerName}/{SemaphoreBlob}'.");
            string leaseId = string.Empty;
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    Tuple<string, string> leaseAction = new Tuple<string, string>("x-ms-lease-action", "acquire");
                    Tuple<string, string> leaseDuration = new Tuple<string, string>("x-ms-lease-duration", "60" /* seconds */);
                    List<Tuple<string, string>> additionalHeaders = new List<Tuple<string, string>>() { leaseAction, leaseDuration };
                    var request = AzureHelper.RequestMessage("PUT", _leaseUrl, AccountName, AccountKey, additionalHeaders);
                    using (HttpResponseMessage response = await AzureHelper.RequestWithRetry(Log, client, request))
                    {
                        leaseId = response.Headers.GetValues("x-ms-lease-id").FirstOrDefault();
                    }
                }
                catch (Exception e)
                {
                    Log.LogErrorFromException(e, true);
                }
            }

            return leaseId;
        }

        private static void AutoRenewLeaseOnBlob(FinalizeBuild instance, string accountName, string accountKey, string containerName, string blob, string leaseId, Microsoft.Build.Utilities.TaskLoggingHelper Log)
        {
            TimeSpan maxWait = TimeSpan.FromSeconds(s_MaxWaitDefault);
            TimeSpan delay = TimeSpan.FromMilliseconds(s_DelayDefault);
            TimeSpan waitFor = maxWait;
            CancellationToken token = instance._cancellationTokenSource.Token;

            while (true)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    Log.LogMessage(MessageImportance.Low, $"Requesting lease for container/blob '{containerName}/{blob}'.");
                    using (HttpClient client = new HttpClient())
                    {
                        Tuple<string, string> leaseAction = new Tuple<string, string>("x-ms-lease-action", "renew");
                        Tuple<string, string> headerLeaseId = new Tuple<string, string>("x-ms-lease-id", leaseId);
                        List<Tuple<string, string>> additionalHeaders = new List<Tuple<string, string>>() { leaseAction, headerLeaseId };
                        var request = AzureHelper.RequestMessage("PUT", GetBlobLeaseRequestUrl(accountName, containerName, blob), accountName, accountKey, additionalHeaders);
                        using (HttpResponseMessage response = AzureHelper.RequestWithRetry(Log, client, request).GetAwaiter().GetResult())
                        {
                            if(!response.IsSuccessStatusCode)
                            {
                                throw new Exception("Unable to acquire lease.");
                            }
                        }
                    }
                    waitFor = maxWait;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Rerying lease renewal on {containerName}, {e.Message}");
                    waitFor = delay;
                }
                token.ThrowIfCancellationRequested();

                Thread.Sleep(waitFor);
            }
        }

        public void ReleaseLeaseOnBlob(string blob, string leaseId)
        {
            // Cancel the lease renewal task since we are about to release the lease.
            ResetLeaseRenewalTaskState();

            using (HttpClient client = new HttpClient())
            {
                Tuple<string, string> leaseAction = new Tuple<string, string>("x-ms-lease-action", "release");
                Tuple<string, string> headerLeaseId = new Tuple<string, string>("x-ms-lease-id", leaseId);
                List<Tuple<string, string>> additionalHeaders = new List<Tuple<string, string>>() { leaseAction, headerLeaseId };
                var request = AzureHelper.RequestMessage("PUT", _leaseUrl, AccountName, AccountKey, additionalHeaders);
                using (HttpResponseMessage response = AzureHelper.RequestWithRetry(Log, client, request).GetAwaiter().GetResult())
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        Log.LogMessage($"Unable to release lease on container/blob {ContainerName}/{blob}.");
                    }
                }

            }
        }


        private static string GetBlobLeaseRequestUrl(string accountName, string containerName, string blob)
        {
            return $"{GetBlobRootRequestUrl(accountName, containerName, blob)}?comp=lease";
        }

        private static string GetBlobRootRequestUrl(string accountName, string containerName, string blob)
        {
            return $"https://{accountName}.blob.core.windows.net/{containerName}/{blob}";
        }

        private void ResetLeaseRenewalTaskState()
        {

            // Cancel the lease renewal task if it was created
            if (_leaseRenewalTask != null)
            {
                _cancellationTokenSource.Cancel();

                // Block until the task ends. It can throw if we cancelled it before it completed.
                try
                {
                    _leaseRenewalTask.Wait();
                }
                catch (Exception)
                {
                    // Ignore the caught exception as it will be expected.
                }

                _leaseRenewalTask = null;
            }
        }
    }
}
