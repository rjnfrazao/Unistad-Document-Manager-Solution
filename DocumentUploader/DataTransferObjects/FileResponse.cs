
using Azure.Storage.Files.Shares.Models;
using StorageLibrary.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DocumentUploader.DataTransferObjects
{
    public class FileResponse 
    {

        public FileResponse(ShareFileItem file)
        {
            //RowKey = entity.RowKey;
            Type = file.GetType().ToString();
            Name = file.Name;
        }


        public string Type { get; set; }

        public string Name { get; set; }


    }
}
