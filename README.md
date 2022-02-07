# Document-Manager-Solution
Application to automate the process to upload, archive and file naming.

# What the application does
Application is used to upload pdf documents from a project, based on some key elements in the document, the application archive the uploaded document into the right folder using the right naming convention for the files. The key elemts the application scans into the pdf document are project name, document type, service name, version number, and document reference number. All these elements are stored into the pages one and two.

# Application Architecture

![Document Manager Architecture](https://github.com/rjnfrazao/Unistad-Document-Manager-Solution/blob/master/Doc Manager Architecture.JPG?raw=true)

# Application Components

# Core Technical elements
- The Web Application is developed using Razor Pages.
- PdfPig is the component used to scan the pdf document.
- Azure Identity Management is used to implement the user authentication into Razor Pazes.
- Azure File Share is used to store the pdf files.

# More technical details of the applicaion
