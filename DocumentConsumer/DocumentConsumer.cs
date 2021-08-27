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


                // Desirialize the Job message
                QueueJobMessage queueMessage = JsonConvert.DeserializeObject<QueueJobMessage>(myQueueItem);

                // (#) Instatiate TableProcessor but inject JobTable object.
                var tableProcessor = new TableProcessor(new JobTable(log, configuration, ConfigSettings.TABLE_PATITION_KEY));

                await tableProcessor.UpdateJobTableWithStatus(log, queueMessage.jobId, 2, "", "");

                log.LogInformation($"[+] Job record {queueMessage.jobId} updated to {JobStatusCode.GetStatus(2)}.");

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
                    uploadedFolder = configuration.GetValue<string>("DevelopmentFileSystemRoot") + ConfigSettings.FILE_SHARE_FOLDER_DELIMITER + uploadedFolder;
                    failedFolder = configuration.GetValue<string>("DevelopmentFileSystemRoot") + ConfigSettings.FILE_SHARE_FOLDER_DELIMITER + failedFolder;
                    targetRootFolder = configuration.GetValue<string>("DevelopmentFileSystemRoot") + ConfigSettings.FILE_SHARE_FOLDER_DELIMITER + targetRootFolder;

                } else
                {
                    // Production use the file share.
                    fileShare = new StorageLibrary.Repositories.FileShare(log, configuration);
 
                }

                // Instantiate object responsible to work out the formated file name. 
                UnistadDocument unistadDoc = new UnistadDocument(stadiumDir, serviceDir, documentTypeDir, edrmsList,
                                                                targetStadiumDir, targetServiceDir, targetDocumentDir);
 
                log.LogInformation($"[+] File share and Unistad Document instatiated completed.");


                // return the file as Stream, initialize a memory stream.
                using (Stream stream = await fileShare.GetFile(uploadedFolder, queueMessage.fileName))
                //using (var memoryStream = new MemoryStream())
                //{

                    // copy the file stream into the memorystream, as PdfDocument requires seek the stream.
                    //await stream.CopyToAsync(memoryStream);
                    //memoryStream.Position = 0;

                    using (PdfDocument pdfDoc = PdfDocument.Open(stream))
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

                //}


                // Add file extension
                documentName = documentName + ".pdf";

                // Uploaded file path
                string uploadedFile = uploadedFolder + ConfigSettings.FILE_SHARE_FOLDER_DELIMITER + queueMessage.fileName;

                // Destination failed file path. Added part of the GUID to avoid duplicatation when saving the failed file.
                failedDocumentName = queueMessage.fileName.Substring(0, queueMessage.fileName.IndexOf("."));
                failedDocumentName = failedDocumentName + "-" + queueMessage.jobId.Substring(queueMessage.jobId.Length - 5) + ".pdf";
                string failedFile = failedFolder + ConfigSettings.FILE_SHARE_FOLDER_DELIMITER + failedDocumentName;

                // Destination file path, in case conversion successfuly completed.
                string destinationFolder = targetRootFolder + ConfigSettings.FILE_SHARE_FOLDER_DELIMITER + targetSubFolder;
                string destinationFile = destinationFolder + ConfigSettings.FILE_SHARE_FOLDER_DELIMITER + documentName;

                // In case not able to work out the destination folder or file name.
                // The process fails. File is moved to failed folder
                if (!unistadDoc.ConversionOk ||
                            await fileShare.FileExists(destinationFolder, documentName))
                {
                    // Process failed
                    log.LogInformation($"[+] Process Failed. Document folder or file name couldn't be worked out.");

                    // remove the extension of the pdf and add GUID (5 last words).


                    // Move the file to the Failed Folder.

                    await fileShare.MoveFileUploaded(uploadedFile, failedFile);


                    // Not able to work out the new file name or destination folder, where the file should be archived.
                    if (!unistadDoc.ConversionOk)
                    {
           
                        // Not able to work out the destination file name.
                        errorMessage = $"Not able to work out the destination name of the document. Error message: {unistadDoc.ConversionErrorMessage}";

                        // Update record job status with status Job FAILED.
                        await tableProcessor.UpdateJobTableWithStatus(log, queueMessage.jobId, 4, errorMessage, failedFile);

                        // PENDING: Update Jobs record with success status.
                        log.LogInformation($"[+] Error: {errorMessage}");

                    }
                    else
                    {
                        // File already exist with the same name at the destination folder.

                        errorMessage = $"The file {documentName} already exists at the destination folder {destinationFolder}. [ERROR: 201]";

                        // Move the uploaded file to the failed file. Both are full path info.
                        await fileShare.MoveFileUploaded(uploadedFile, failedFile);

                        // Update record job status with status Job FAILED.
                        await tableProcessor.UpdateJobTableWithStatus(log, queueMessage.jobId, 4, errorMessage, failedFile);

                        // PENDING: Update Jobs record with success status.
                        log.LogInformation($"[+] Error: {errorMessage}");
                    }
                }
                else
                {
                    // Process completed successfully

                    // Move the uploaded file to the destination file. Both are full path info.
                    if (await fileShare.MoveFileUploaded(uploadedFile, destinationFile))
                    {

                        // Update record job status with status Job Completed.
                        await tableProcessor.UpdateJobTableWithStatus(log, queueMessage.jobId, 3, "", destinationFile);

                        log.LogInformation($"[+] Process completed sucessfuly.");
                    } 
                    else
                    {
                        errorMessage = $"Internal Error. Not able to move the file {uploadedFile} successfuly converted to the destination folder {destinationFile}";

                        // Move the uploaded file to the failed file. Both are full path info.
                        await fileShare.MoveFileUploaded(uploadedFile, failedFile);

                        // Update record job status with status Job FAILED.
                        await tableProcessor.UpdateJobTableWithStatus(log, queueMessage.jobId, 4, errorMessage, failedFile);

                        log.LogInformation($"[+] Process failed. Error: {errorMessage}");
                    }

                }


            }
            catch (Exception e)
            {

                log.LogError($"[+] Process Failed. Unknown exception [Error:200]. Error message: {e.Message}");
            }
            


        }
    }
}
