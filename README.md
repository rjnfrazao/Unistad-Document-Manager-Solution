# Document Manager Solution
Application to automate the process to upload, archive and file naming.

# What the application does
Application is used to upload pdf documents from a project, based on some key elements in the document, the application archive the uploaded document into the right folder using the right naming convention for the files. The key elements in the pdf document are client name, document type, service name, version number, and document reference number. All these elements are found in the pages one and two.

# Application Architecture

Application architecture was designed based on the Microservices pattern, where there is a separation between the several components of the solution. The separation of concerns was on o high level: Front End (Service to implement the user front end), Web APIs (Service to create the job), and Functional App (Service to process the job). The level of fragmentation could be much higher but this was just the first step.

Another architecture pattern applied was the Event Driven approach, as it was separated the document upload process from the document consuming process, a new message queue event is the trigger between both process. This approach for example could allow in high intensive usage allow to scale up the Upload process (Web APIs) independently of the Consuming process (Functional App). 

![Document Manager Architecture](https://github.com/rjnfrazao/Unistad-Document-Manager-Solution/blob/master/Doc%20Manager%20Architecture.JPG?raw=true)

# Application Components

- Web Application - Web front end application used to upload the files. This application uses Microsoft Identity Plataform to authenticate the users. This application shows also the status of each file uploaded.

- Web APIs - APIs invoked by the Web Application. Such as upload document api, get document api, list jobs api, etc. 
  - The Upload Document API is responsible to store the uploaded file into the uploaded folder, create record into the job table, and add a message to the queue. 
  
- Functional App - Process the files uploaded. It opens the file, scan for key elements, archive the file according the business rules. The functional app is triggered when a new message is added to the queue. 

- Azure Storage :
  - File Share : Where the files are archived. The following core folders are used : 
    - uploaded : Every file uploaded are temporaly stored in this folder. 
    - failed : The file is moved to this folder, when the folder destination or file name can't be defined. 
    - documents : When the files can be archived, they are moved to this folder into the right subfolder using the right file name. 
  - Storage Queue : Queue used to store a message per document uploaded.
  - Storage Table : Table used to keep track of all jobs. There is one job for each file uploaded.  

# Core Technical elements
- Azure Cloud host solution.
- The Web Application is developed using Razor Pages.
- Web Api developed using .Net Core.
- PdfPig is the component used to scan the pdf document.
- Microsoft Identity Platform using Azure AD is used to implement the user authentication into Razor Pazes.
- Azure File Share is used to store the pdf files.

# More technical details of the applicaion
- The application keeps a data mapping file with all configuration required to define the destination folder and file name to be used when the document is archived, based on the key elements found in the file.
- The application was designed based on the Microservices design.

# Future Improvements
- Implement Containers using Docker. I started, however I was struglling on activate the SSL certificate in the Kestrel Web Service inside the container image.
- Implement automated tests.
- Implement CI/CD.
