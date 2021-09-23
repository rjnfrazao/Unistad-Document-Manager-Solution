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
            string uploadedFile = "";               // full path (uploadedFolder + queueMessage.fileName)           
            string failedFolder = "";
            string failedDocumentName = "";
            string failedFile = "";                 // full path (faildFolder + failedDocumentName)

            string targetRootFolder = "";
            string targetSubFolder = "";
            string documentName = "";
            string errorMessage = "";

            TableProcessor tableProcessor = null;
            QueueJobMessage queueMessage = null;

            IFileShare fileShare=null;

            try
            {
                log.LogInformation($"Start processing. Queue trigger function processed: {myQueueItem}");

                // Instantiate the configuration object
                var configuration = new ConfigurationBuilder()
                        .SetBasePath(context.FunctionAppDirectory)
                        .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile("DictionaryMapping.settings.json", optional: true, reloadOnChange: true)
                        .AddUserSecrets("b57a545a-6af3-4a60-91c2-c2b435445f69")
                        .AddEnvironmentVariables()
                        .Build();

                // Desirialize the Job message
                queueMessage = JsonConvert.DeserializeObject<QueueJobMessage>(myQueueItem);

                // (#) Instatiate TableProcessor but inject JobTable object.
                tableProcessor = new TableProcessor(new JobTable(log, configuration, ConfigSettings.TABLE_PARTITION_KEY));

                // Folders where the files are located or stored.
                uploadedFolder = ConfigSettings.FILE_SHARE_UPLOADED_FOLDER;
                failedFolder = ConfigSettings.FILE_SHARE_FAILED_FOLDER;
                targetRootFolder = ConfigSettings.FILE_SHARE_UNISTAD_FOLDER;


                // There are no FileShare in Azurite, so in development use FileSystem to store the files.
                if (configuration.GetValue<string>("AzureWebJobsStorage") == "UseDevelopmentStorage=true")
                {
                    // Development use the file system.
                    fileShare = new StorageLibrary.Repositories.FileSystem(log, configuration);

                    // In case of development environment. Add the root folder location in the file system.
                    uploadedFolder = Path.Combine(configuration.GetValue<string>("DevelopmentFileSystemRoot"),uploadedFolder);
                    failedFolder = Path.Combine(configuration.GetValue<string>("DevelopmentFileSystemRoot"),failedFolder);
                    targetRootFolder = Path.Combine(configuration.GetValue<string>("DevelopmentFileSystemRoot"), targetRootFolder); // removed  + ConfigSettings.FILE_SHARE_FOLDER_DELIMITER + 

                }
                else
                {
                    // Production use the file share.
                    fileShare = new StorageLibrary.Repositories.FileShare(log, configuration);

                }



                // Initialize variables required to move files.
                uploadedFile = Path.Combine(uploadedFolder, queueMessage.fileName);      // Uploaded file path (removed  + ConfigSettings.FILE_SHARE_FOLDER_DELIMITER +) 

                // [+] Destination failed file path. Added part of the GUID to avoid duplicatation when saving the failed file.
                failedDocumentName = queueMessage.fileName.Substring(0, queueMessage.fileName.IndexOf("."));
                failedDocumentName = failedDocumentName + "-" + queueMessage.jobId.Substring(queueMessage.jobId.Length - 5) + ".pdf";
                failedFile = Path.Combine(failedFolder, failedDocumentName);






                // Update job status to job running
                await tableProcessor.UpdateJobTableWithStatus(log, queueMessage.jobId, 2, "", "");

                // Load the dictionaries, where the mapping between value and code is stored (Stadium, Service, and Document Type).
                Dictionary<string, string> stadiumDir = Utils.Library.InitializeDictionary(configuration, "Stadium");
                Dictionary<string, string> serviceDir = Utils.Library.InitializeDictionary(configuration, "Service");
                Dictionary<string, string> documentTypeDir = Utils.Library.InitializeDictionary(configuration, "DocumentType");

                // Load the list of patterns used to identify a valid EDRMS number used by documents.    
                List<string> edrmsList = Utils.Library.InitializeList(configuration, "Edrms");


                // Load the dictionaries to work out the target folder where the file will be stored.
                Dictionary<string, string> targetStadiumDir = Utils.Library.InitializeDictionary(configuration, "StadiumFolder");
                Dictionary<string, string> targetServiceDir = Utils.Library.InitializeDictionary(configuration, "ServiceFolder");
                Dictionary<string, string> targetDocumentDir = Utils.Library.InitializeDictionary(configuration, "DocumentFolder");


                log.LogInformation($"[+] Job record {queueMessage.jobId} updated to {JobStatusCode.GetStatus(2)}.");



                // Instantiate object responsible to work out the formated file name. 
                UnistadDocument unistadDoc = new UnistadDocument(stadiumDir, serviceDir, documentTypeDir, edrmsList,
                                                                targetStadiumDir, targetServiceDir, targetDocumentDir);
 
                log.LogInformation($"[+] File share and Document instatiated completed.");


                // Start process to workout file name and folder.
                // Get the file as stream and instatiate pdf object
                using (Stream stream = await fileShare.GetFile(uploadedFolder, queueMessage.fileName))
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

                // Add file extension
                documentName = $"{documentName}.pdf";

                // Destination file path, in case conversion successfuly completed.
                string destinationFolder = Path.Combine(targetRootFolder, targetSubFolder);
                string destinationFile = Path.Combine(destinationFolder, documentName);

                // In case not able to work out the destination folder or file name.
                // The process fails. File is moved to failed folder
                if (!unistadDoc.ConversionOk ||
                            await fileShare.FileExists(destinationFolder, documentName))
                {
                    // Process failed
                    log.LogInformation($"[+] Process Failed. Document folder or file name couldn't be worked out.");

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
                errorMessage = $"[+] Process Failed. Unknown exception [Error:200]. Error message: {e.Message}";

                log.LogError(errorMessage);

                // In case unepected error happen, and tableProcessor was already initialized
                // Assure the file uploaded is moved to failed folder and job status record is updated to process failed.
                if (tableProcessor != null)
                {
                    
                    // Move the uploaded file to the failed file. Both are full path info.
                    await fileShare.MoveFileUploaded(uploadedFile, failedFile);

                    // Update record job status with status Job FAILED.
                    await tableProcessor.UpdateJobTableWithStatus(log, queueMessage.jobId, 4, errorMessage, failedFile);
                }

            } 
            


        }
    }
}
