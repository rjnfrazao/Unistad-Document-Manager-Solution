using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using ConfigurationLibrary;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using StorageLibrary.DataTransferObjects;
using Newtonsoft.Json;

using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Threading.Tasks;
using StorageLibrary.Repositories;
using System.IO;
using UnistadDocumentLibrary;
using UnistadDocumentLibrary.Exceptions;
using StorageLibrary.Library;

namespace DocumentConsumer
{
    public class DocumentConsumer
    {
        [FunctionName("DocumentConsumerFunction")]
        public async Task Run([QueueTrigger(ConfigSettings.QUEUE_TOPROCESS_NAME, Connection = ConfigSettings.QUEUE_CONNECTIONSTRING_NAME)]string myQueueItem, ExecutionContext context, ILogger log)
        {
            string uploadedFolder = "";
            string failedFolder = "";
            string targetRootFolder = "";
            string targetSubFolder = "";
            string documentName = "";
            string errorMessage = "";
            string failedDocumentName = "";

            IFileShare fileShare;

            try
            {
                log.LogInformation($"Start processing. Queue trigger function processed: {myQueueItem}");

                var configuration = new ConfigurationBuilder()
                                        .SetBasePath(context.FunctionAppDirectory)
                                        .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                                        .AddUserSecrets("b57a545a-6af3-4a60-91c2-c2b435445f69")
                                        .AddEnvironmentVariables()
                                        .Build();

                // Load the dictionaries, where the mapping between value and code is stored (Stadium, Service, and Document Type).
                Dictionary<string, string> stadiumDir = Utils.Library.InitializeDictionary(configuration, "Stadium");
                Dictionary<string, string> serviceDir = Utils.Library.InitializeDictionary(configuration, "Service");
                Dictionary<string, string> documentTypeDir = Utils.Library.InitializeDictionary(configuration, "DocumentType");

                // Load the list of patterns used to identify a valid EDRMS number used by UNISTAD documents.    
                List<string> edrmsList = Utils.Library.InitializeList(configuration, "Edrms");


                // Load the dictionaries to work out the target folder where the file will be stored.
                Dictionary<string, string> targetStadiumDir = Utils.Library.InitializeDictionary(configuration, "StadiumFolder");
                Dictionary<string, string> targetServiceDir = Utils.Library.InitializeDictionary(configuration, "ServiceFolder");
                Dictionary<string, string> targetDocumentDir = Utils.Library.InitializeDictionary(configuration, "DocumentFolder");


                // Instantiate object responsible to work out the formated file name. 
                UnistadDocument unistadDoc = new UnistadDocument(stadiumDir, serviceDir, documentTypeDir, edrmsList,
                                                                targetStadiumDir, targetServiceDir, targetDocumentDir);
                log.LogInformation($"Unistad document instatiated.");

                // Desirialize the Job message
                QueueJobMessage queueMessage = JsonConvert.DeserializeObject<QueueJobMessage>(myQueueItem);

                // (#) Instatiate TableProcessor but inject JobTable object.
                var tableProcessor = new TableProcessor(new JobTable(log, configuration, ConfigSettings.TABLE_PATITION_KEY));

                await tableProcessor.UpdateJobTableWithStatus(log, queueMessage.jobId, 2, "", "");

                log.LogInformation($"Job record {queueMessage.jobId} updated to {JobStatusCode.GetStatus(2)}");

                // Folders where the files are located or stored.
                uploadedFolder = ConfigSettings.FILE_SHARE_UPLOADED_FOLDER;
                failedFolder = ConfigSettings.FILE_SHARE_FAILED_FOLDER;
                targetRootFolder = ConfigSettings.FILE_SHARE_UNISTAD_FOLDER;

                // There are no FileShare in Azurite, so in development use FileSystem to store the files.
                if (configuration.GetValue<string>("AzureWebJobsStorage") == "UseDevelopmentStorage=true")
                {
                    // Development use the file system.
                    fileShare = new StorageLibrary.Repositories.FileSystem(log, configuration);

                    // Add the root folder location in the file system.
                    uploadedFolder = configuration.GetValue<string>("DevelopmentFileSystemRoot") + uploadedFolder;
                    failedFolder = configuration.GetValue<string>("DevelopmentFileSystemRoot") + failedFolder;
                    targetRootFolder = configuration.GetValue<string>("DevelopmentFileSystemRoot") + targetRootFolder;
                } else
                {
                    // Production use the file share.
                    fileShare = new StorageLibrary.Repositories.FileShare(log, configuration);

                }

                log.LogInformation($"File share/system instatiated. Document name and destination folder will be worked out.");


                // return the file as Stream, initialize a memory stream.
                using (Stream stream = await fileShare.GetFile(uploadedFolder, queueMessage.fileName))
                using (var memoryStream = new MemoryStream())
                {

                    // copy the file stream into the memorystream, as PdfDocument requires seek the stream.
                    await stream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    using (PdfDocument pdfDoc = PdfDocument.Open(memoryStream))
                    {
                        int pageCount = pdfDoc.NumberOfPages;

                        // Get first page
                        Page page = pdfDoc.GetPage(1);

                        // Initialize Cover Page
                        unistadDoc.CoverPage = page.Text;

                        // Get second page
                        page = pdfDoc.GetPage(2);

                        // Initialize Second Page
                        unistadDoc.SecondPage = page.Text;

                        // Get the document name, based on the content of the Cover Page and Second Page.
                        (targetSubFolder, documentName) = unistadDoc.getDocumentName();

                    }

                }


                // In case not able to work out the destination folder or file name.
                // The process fails. File is moved to failed folder.
                if (!unistadDoc.ConversionOk ||
                            await fileShare.FileExists(targetRootFolder + targetSubFolder, documentName + ".pdf"))
                {
                    // Process failed

                    // remove the extension of the pdf and add GUID (5 last words).
                    failedDocumentName = queueMessage.fileName.Substring(0, queueMessage.fileName.IndexOf("."));
                    failedDocumentName = failedDocumentName + "-" + queueMessage.jobId.Substring(queueMessage.jobId.Length - 5) + ".pdf";

                    // Move the file to the Failed Folder.
                    await fileShare.MoveFileUploaded(uploadedFolder + queueMessage.fileName, failedFolder + failedDocumentName);

                    // PENDING: Update Jobs record with failure.

                    if (!unistadDoc.ConversionOk)
                    {
           
                        // Not able to work out the destination file name.
                        errorMessage = $"Not able to work out the destination name of the document. Error message: {unistadDoc.ConversionErrorMessage}";

                        // Update record job status with status Job FAILED.
                        await tableProcessor.UpdateJobTableWithStatus(log, queueMessage.jobId, 4, errorMessage, failedFolder + failedDocumentName);

                        // PENDING: Update Jobs record with success status.
                        log.LogInformation($"Process Failed. {errorMessage}");

                    }
                    else
                    {
                        // Not able to work out the destination folder, where the file should be archived.
                        errorMessage = $"The file {documentName} already exists at the destination folder {targetRootFolder}{targetSubFolder}. [ERROR: 201]";
 
                        // Update record job status with status Job FAILED.
                        await tableProcessor.UpdateJobTableWithStatus(log, queueMessage.jobId, 4, errorMessage, failedFolder + failedDocumentName);

                        // PENDING: Update Jobs record with success status.
                        log.LogInformation($"Process Failed. {errorMessage}");
                    }
                }
                else
                {
                    // Process completed successfully

                    // add the file extension.
                    documentName = documentName + ".pdf";

                    // Everything is fine. Move the file in the destination folder using the destination file name.
                    if (await fileShare.MoveFileUploaded(uploadedFolder + queueMessage.fileName, targetRootFolder + targetSubFolder + documentName))
                    {
                        
                        // Update record job status with status Job Completed.
                        await tableProcessor.UpdateJobTableWithStatus(log, queueMessage.jobId, 3, "", targetRootFolder + targetSubFolder + documentName);

                        log.LogInformation($"Process Completed.");
                    } 
                    else
                    {
                        errorMessage = $"Process Failed. Not able to move the processed file to the destination folder";

                        // Update record job status with status Job FAILED.
                        await tableProcessor.UpdateJobTableWithStatus(log, queueMessage.jobId, 4, errorMessage, failedFolder + failedDocumentName);


                        // PENDING: Update Jobs record with success status.
                        log.LogInformation($"Process Failed. {errorMessage}");
                    }

                }


            }
            catch (Exception e)
            {

                log.LogError($"Process Failed. Unknown exception [Error:200]. Error message: {e.Message}");
            }
            


        }
    }
}
