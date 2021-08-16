using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace WebRole1.Data
{
    public class PastebinDatabase
    {
        private static readonly Lazy<PastebinDatabase>
        lazy = new Lazy<PastebinDatabase>
        (() => new PastebinDatabase());

        public static PastebinDatabase Instance { get { return lazy.Value; } }

        private PastebinDatabase()
        {
        }

        // The Azure Cosmos DB endpoint.
        private static readonly string EndpointUri = "https://santanu-pastebin.documents.azure.com:443/";

        // The primary key for the Azure Cosmos account.
        private static readonly string PrimaryKey = "";

        // The Cosmos client instance
        private CosmosClient cosmosClient;

        // The database we will create
        private Database database;

        // The container we will create.
        private Container container;

        // The name of the database and container we will create
        private string databaseId = "pastebin-db";
        private string containerId = "pastebin-container";


        /// <summary>
        /// Create the database if it does not exist
        /// </summary>
        private async Task CreateDatabaseAsync()
        {
            // Create a new database
            this.database = await this.cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            Console.WriteLine("Created Database: {0}\n", this.database.Id);
        }

        /// <summary>
        /// Create the container if it does not exist. 
        /// Specifiy "/LastName" as the partition key since we're storing family information, to ensure good distribution of requests and storage.
        /// </summary>
        /// <returns></returns>
        private async Task CreateContainerAsync()
        {
            // Create a new container
            this.container = await this.database.CreateContainerIfNotExistsAsync(containerId, "/ShortUrlStub");
            Console.WriteLine("Created Container: {0}\n", this.container.Id);
        }

        bool isInitialized = false;

        public async Task Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            isInitialized = true;

            try
            {
                // Create a new instance of the Cosmos Client in Gateway mode
                this.cosmosClient = new CosmosClient(EndpointUri, PrimaryKey, new CosmosClientOptions()
                {
                    ConnectionMode = ConnectionMode.Gateway
                });

                await this.CreateDatabaseAsync();
                await this.CreateContainerAsync();
            }
            catch (CosmosException de)
            {
                Exception baseException = de.GetBaseException();
                Console.WriteLine("{0} error occurred: {1}\n", de.StatusCode, de);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}\n", e);
            }
        }


        /// <summary>
        /// Run a query (using Azure Cosmos DB SQL syntax) against the container
        /// </summary>
        public async Task<string> QueryPastebinItemAsync(string shortUrlStub)
        {
            await Initialize();

            var sqlQueryText = $"SELECT * FROM c WHERE c.ShortUrlStub = '{shortUrlStub}'";

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            FeedIterator<PastebinItem> queryResultSetIterator = this.container.GetItemQueryIterator<PastebinItem>(queryDefinition);

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<PastebinItem> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (PastebinItem pastebinItem in currentResultSet)
                {
                    return pastebinItem.PasteData;
                }
            }

            return null;
        }


        public async Task<string> AddItemToContainerAsync(string pasteData)
        {
            await Initialize();

            int maxRetries = 5;

            //Generate random url stub
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    Guid g = Guid.NewGuid();
                    string proposedUrlStub = g.ToString().Split('-')[0];

                    PastebinItem pastebinItem = new PastebinItem();
                    pastebinItem.ShortUrlStub = proposedUrlStub;
                    pastebinItem.PasteData = pasteData;
                    pastebinItem.Id = proposedUrlStub;

                    ItemResponse<PastebinItem> response = await this.container.CreateItemAsync<PastebinItem>(pastebinItem);
                    return proposedUrlStub;
                }
                catch (CosmosException ex)
                {
                    Console.WriteLine(ex);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            return null;
        }
    }
}