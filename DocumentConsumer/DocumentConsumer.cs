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
       
            IFileShare fileShare;

            try
            {
                log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");

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


                // Desirialize the Job message
                QueueJobMessage queueMessage = JsonConvert.DeserializeObject<QueueJobMessage>(myQueueItem);

                log.LogInformation($"Queued message: {queueMessage.ToString()}");

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
                

                //Stream stream = document.SaveFile(queueMessage.fileName);

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
                        (string targetSubFolder, string documentName) = unistadDoc.getDocumentName();


                        // In case any conversion failed, the process is incomplete, so raise an exception
                        // or in case the same file already exists in the target location.
                        if (!unistadDoc.ConversionOk || 
                            await fileShare.FileExists(targetRootFolder + targetSubFolder, documentName + ".pdf"))  
                        {

                            // file has added GUID (5 last words) to the file name, as can have duplication.
                            documentName = documentName + "-" + queueMessage.jobId.Substring(queueMessage.jobId.Length-5) + ".pdf";
                            
                            // Operation failed. Not able to define file name or target location.
                            // Save the file in the Failed Folder.
                            await fileShare.SaveFileUploaded(failedFolder, documentName, memoryStream);

                            // Delete the file from the uploaded foldr
                            await fileShare.DeleteFile(uploadedFolder, documentName);

                            // PENDING: Update Jobs record with failure.

                            if (!unistadDoc.ConversionOk) { throw new Exception(unistadDoc.ConversionErrorMessage); } 
                            else { throw new Exception($"The file {documentName} already exists at the target folder {targetRootFolder}. [ERROR: 200]")}
                        }

                        // Everything is fine. Save the file in the target location.
                        await fileShare.SaveFileUploaded(targetRootFolder + targetSubFolder, documentName , memoryStream);

                        // Delete the file from the original location
                        await fileShare.DeleteFile(uploadedFolder, documentName);

                        // PENDING: Update Jobs record with success status.

                        log.LogInformation($"Document Name: {documentName} archived at {targetRootFolder + targetSubFolder}.");



                    }
                }

            }
            catch (Exception e)
            {
                log.LogError($"Error exception: {e.Message}");
            }

        }
    }
}
