using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BlobStorage.Controllers
{
    public class UploadArquivosController : Controller
    {

        /// <summary>
        /// Variavel de  informação de configs
        /// </summary>
        private readonly IConfiguration _config;

        /// <summary>
        /// Container Trabalhado
        /// </summary>
        private CloudBlobContainer _container;

        /// <summary>
        /// Construtor do Controlle
        /// </summary>
        /// <param name="config"></param>
        public UploadArquivosController(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Pagina Inicial do Upload
        /// </summary>
        /// <returns>View de Upload</returns>
        [HttpGet]
        public async Task<IActionResult> Index()
        {

            ViewBag.listaArquivos = await ListContainersAsync();
            return View();
        }

        /// <summary>
        /// Metodo Post de Envio de Arquivo
        /// </summary>
        /// <param name="arquivo">Arquivo Enviado</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> EnviaImagem(IFormFile arquivo)
        {
            ConfiguraContainer();
            if (arquivo == null)
            {
                return null;
            }
            string caminhoFinal;
            try
            {
                //caso o container não exista cria um, deixei um no config, aqui pode entrar uma regra tipo USUARIO_LOGADO_1
                //então criaria o container para esse USUARIO_LOGADO_1 ai tem que mudar no ConfiguraContainer(), pq lá pego o nome fixo do appsettings
                if (await _container.CreateIfNotExistsAsync())
                {
                    //Da permisão
                    await _container.SetPermissionsAsync(
                        new BlobContainerPermissions
                        {
                            PublicAccess = BlobContainerPublicAccessType.Blob
                        }
                        );
                }


                CloudBlockBlob cloudBlockBlob = _container.GetBlockBlobReference(arquivo.FileName);
                //tipo do arquivo
                cloudBlockBlob.Properties.ContentType = arquivo.ContentType;

                //parada que vc pediu para renomar
                if (await cloudBlockBlob.ExistsAsync())
                    await Suporte.RenameAsync(_container, arquivo.FileName);

                //salva o arquivo
                using (var stream = arquivo.OpenReadStream())
                    await cloudBlockBlob.UploadFromStreamAsync(stream);
                //url do arquivo já hospedado
                caminhoFinal = cloudBlockBlob.Uri.ToString();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Cria Conexão com o Azure
        /// </summary>
        /// <returns>Variavel com ccontrole de conexão</returns>
        private CloudStorageAccount MontaConnection()
        {
            string conta = _config.GetValue<string>("AzureInfo:NomeStorage");
            string chave = _config.GetValue<string>("AzureInfo:KeyStorage");

            string connectionString = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", conta, chave);
            return CloudStorageAccount.Parse(connectionString);
        }

        private void ConfiguraContainer()
        {
            //cria conexão
            CloudStorageAccount cloudStorageAccount = MontaConnection();
            //cria o cliente com base nas informaç~~oes da conexão
            CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            //pega o container que vai ser trabalhado
            _container = cloudBlobClient.GetContainerReference(_config.GetValue<string>("AzureInfo:container"));
        }

        public async Task<List<string[]>> ListContainersAsync()
        {
            ConfiguraContainer();
            BlobContinuationToken continuationToken = null;
            List<CloudBlockBlob> results = new List<CloudBlockBlob>();
            List<string[]> resultsNomeArquivos = new List<string[]>();
            do
            {
                var response = await _container.ListBlobsSegmentedAsync(continuationToken);
                continuationToken = response.ContinuationToken;
                foreach (var item in response.Results.ToList())
                    results.Add((CloudBlockBlob)item);
            }
            while (continuationToken != null);

            foreach (var item in results)
                resultsNomeArquivos.Add(new string[] { item.Name, item.Uri.ToString() });

            return resultsNomeArquivos;
        }     
        
        public async Task<IActionResult> ApagaArquivo(string arquivo)
        {
            ConfiguraContainer();
            var blob = _container.GetBlockBlobReference(arquivo);
            await blob.DeleteIfExistsAsync();

            return RedirectToAction("Index");
        }

    }
}
