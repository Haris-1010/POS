using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Web;

namespace CoreMedixBeta.DbModels
{
    public class BlobUploader
    {
        public static string UploadTo(string folder_name, string file_name, Stream ms)
        { 
            string basePath = HttpContext.Current?.Server.MapPath("~/Uploads");
            if (string.IsNullOrEmpty(basePath))
            { 
                basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Uploads");
            }
             
            string folderPath = Path.Combine(basePath, folder_name ?? "default");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
             
            string filePath = Path.Combine(folderPath, file_name ?? Guid.NewGuid().ToString());
             
            ms.Seek(0, SeekOrigin.Begin);
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                ms.CopyTo(fs);
            }
             
            return $"/Uploads/{folder_name}/{file_name}";
        }


        public static void Delete(string folder_name, string file_name)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(folder_name);
            container.CreateIfNotExists();
            container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(file_name);
            blockBlob.DeleteIfExists();
        }

        internal static string UploadProfileImage(string Image, string Username)
        {
            ImageFile imgfile = new ImageFile(Image);
            string FolderName = GetFolderName(Username);
            string SaveLocation = ServerConfig.BlobPath + GetFolderName(Username) + "/" + Username + '.' + imgfile.ImageExtention;
            MemoryStream ms = new MemoryStream(Convert.FromBase64String(imgfile.ImageData));
            UploadTo(FolderName, SaveLocation, ms);
            return SaveLocation;
        }

        public static string UploadDocument(string Username, string FileName, Stream ms)
        {
            string FolderName = GetFolderName(Username);

            string SaveLocation = ServerConfig.BlobPath + FolderName + "/" + FileName.Split('.')[0] + DateTime.Now.ToString("yyyMMddHHmmss") + "." + FileName.Split('.')[1];
            UploadTo(FolderName, SaveLocation, ms);
            return SaveLocation;
        }

        public static string GetFolderName(string Username)
        {
            return Username.Replace(".", "-").ToLower();
        }
    }

    public class ImageFile
    {
        public string ImageData { get; set; }
        public string ImageExtention { get; set; }

        public ImageFile(string Image)
        {
            this.ImageData = Image.Split(',')[1];
            this.ImageExtention = Image.Split(',')[0].Split('/')[1].Split(';')[0];
        }
    }
}