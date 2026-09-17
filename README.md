# ABC Retail - Cloud Architecture & Serverless Backend

**Author:** Nkosinathi Zimkhona Mabena  
**Institution:** The Independent Institute of Education (IIE) Rosebank College  
**Project Type:** ASP.NET Core MVC & Azure Functions  

## 📌 Project Overview
This project is a cloud-integrated web application built for ABC Retail. It demonstrates a decoupled, serverless architecture using an ASP.NET Core MVC frontend and an Azure Functions backend. The system leverages various Azure Storage services to manage structured data, media files, background processing, and file logging.

## ☁️ Azure Services Utilized
*   **Azure Table Storage:** Stores structured NoSQL data for Customers and Products dynamically using entity mapping.
*   **Azure Blob Storage:** Handles the upload, storage, and retrieval of binary product images.
*   **Azure Queue Storage:** Captures order transactions asynchronously to decouple the frontend UI from heavy backend processing.
*   **Azure Files:** Acts as a centralized cloud file share, generating and storing digital `.txt` receipts for processed orders.
*   **Azure Functions (Serverless):**
    *   *HTTP Triggers:* Ingests API requests from the frontend to securely push items to Table and Blob storage.
    *   *Queue Triggers:* A background worker (`ProcessOrderQueue`) that listens for new messages, updates table statuses to "Processed", and writes receipts to Azure Files.

## 🚀 How to Run Locally
To run this solution locally, you must launch both projects simultaneously.

1. **Configure Connection Strings:**
   * Open `ABCRetail.AzureStorage/appsettings.json` and paste your Azure Storage Connection String.
   * Open `AzureRetail.Functions/local.settings.json` and update both `AzureWebJobsStorage` and `AzureStorageConnectionString` with the exact same connection string.
2. **Set Multiple Startup Projects:**
   * Right-click the Solution in Visual Studio -> **Properties** -> **Startup Project**.
   * Select **Multiple startup projects** and set both `ABCRetail.AzureStorage` and `AzureRetail.Functions` to **Start**.
3. Press **F5** to run. The Functions terminal will launch on `localhost:7193` and the MVC web app will connect to it.

## 🌐 Deployment Notes
A live cloud deployment attempt was made for the frontend via **Azure App Service (Windows)**. 
*   **Blobs:** The deployed application successfully retrieves and renders images directly from Azure Blob Storage. 
*   **Functions Routing:** Because the Azure Functions backend remains running in a local debugging state for this phase of the project, submitting forms on the live Azure App Service will yield an expected socket connection error pointing to `localhost:7193`. This intentionally demonstrates the decoupled nature of the UI and the API.
