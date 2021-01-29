using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Threading.Tasks;

namespace BlobStorage
{
    public static class Suporte
    {
        public static async Task RenameAsync(this CloudBlobContainer container, string oldName)
        {
            try
            {
                string newName = oldName.Substring(0, oldName.LastIndexOf('.')) + DateTime.Now.ToString().Replace('/', '_') + oldName.Substring(oldName.LastIndexOf('.'));

                CloudBlockBlob source = (CloudBlockBlob)await container.GetBlobReferenceFromServerAsync(oldName);
                CloudBlockBlob target = container.GetBlockBlobReference(newName);


                await target.StartCopyAsync(source);

                while (target.CopyState.Status == CopyStatus.Pending)
                    await Task.Delay(100);

                if (target.CopyState.Status != CopyStatus.Success)
                    throw new Exception("Rename failed: " + target.CopyState.Status);

                await source.DeleteAsync();
            }
            catch (Exception ex)
            {

                throw ex;
            }           
        }
    }
}
