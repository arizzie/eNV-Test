#  eVN VIN Decoder

A full-stack application for decoding VINs, processing vehicle data from CSV imports, and managing vehicle information. This project leverages Azure Functions for a serverless backend, Entity Framework Core for data persistence, and Next.js for a responsive frontend.

## 🚀 Key Features

* **VIN Decoding API:** Exposed via Azure Functions for real-time VIN information retrieval.
* **CSV Import Workflow:** Utilizes Azure Durable Functions (Orchestrator) to handle robust, scalable CSV file processing for VIN data.
    * **Orchestration Logic:** Manages the flow of parsing, processing, and saving VIN data.
    * **Activity Functions:** Perform individual tasks such as VIN decoding, data transformation, and database persistence.
* **Data Management:** Centralized vehicle and variable data storage using EF Core.
* **Modern Frontend:** A Next.js 15 application provides a user-friendly interface for interacting with the backend.

## 📁 Project Structure

The solution is organized into the following key projects:

* **`./Backend`**:
    * **.NET Azure Isolated Functions:** The serverless backend API.
        * `VinApi.cs`: Handles HTTP GET operations for VIN decoding and vehicle retrieval.
        * `ProcessVinStarter.cs`: Initiates the Durable Functions orchestration for CSV imports.
        * `ProcessVinOrchestration.cs`: The orchestrator function that delegates and manages processing tasks.
        * `ProcessVinActivities.cs`: Contains the individual activity functions (e.g., parsing, processing, saving data).
* **`./Data`**:
    * **EF Core Class Library:** Manages database interactions.
        * `/Models`: Defines the database entities (e.g., `Vehicle`, `AdditionalVehicleInfo`, `VehicleVariable`).
        * `/Repository`: Contains the data access logic and repository implementations.
        * `EvnContext.cs`: The Entity Framework Core `DbContext` for database connectivity and schema management.
* **`./Frontend/evn`**:
    * **Next.js 15 Application:** The modern frontend user interface.

## ⚙️ How to Run the Project

Follow these steps to get the project up and running locally.

### 1. Backend Setup (Azure Functions - `./Backend`)

The backend relies on Azure Blob Storage for CSV imports and an SQL database for data persistence.

#### Blob Storage Configuration

1.  **Azure Blob Storage:** Set up an Azure Storage Account and create a Blob Container.
    * *Tip:* For local development, consider using [Azure Storage Emulator (Azurite)](https://learn.microsoft.com/en-us/azure/storage/common/storage-explorer?tabs=linux#install-and-run-azurite-storage-emulator).
2.  **`local.settings.json`:** Add your Blob Storage connection string.
    * A sample `local.settings.json` file is provided in the `.\Backend` directory.
    * The application will allow you to specify the container and file name at runtime.
    * **Default Blob Container:** `[Specify Default Container Name Here]`
    * **Default File Name:** `[Specify Default File Name Here]` (e.g., `sample-vins.csv`)

#### Database Setup (SQL Server / Azure SQL Database)

You'll need an SQL Server instance (local or Azure SQL Database) and its connection string.

1.  **`local.settings.json`:** Add your database connection string to this file under the `ConnectionStrings` section.
    * Example entry: `"SqlDbConnection": "Server=tcp:...;Database=...;User ID=...;Password=..."`

2.  **Apply Migrations (Create Database Schema):**

    * **Using Visual Studio 2022:**
        1.  Open the **Package Manager Console** (View > Other Windows > Package Manager Console).
        2.  Set the default project to your `.\Data` project.
        3.  Set an environment variable for your connection string (replace `your_connection_string_here`):
            ```powershell
            $env:DbConnection="your_connection_string_here"
            ```
        4.  Run the database update command:
            ```powershell
            Update-Database
            ```

    * **Using VS Code (Terminal):**
        1.  Open the integrated terminal (Ctrl+`).
        2.  Navigate to the repository root: `cd <repository_root>`
        3.  Set an environment variable for your connection string (replace `your_connection_string_here`):
            ```bash
            $env:DbConnection="your_connection_string_here"
            ```
            *(Note: For PowerShell, use `$env:DbConnection="your_connection_string_here"`; for Bash/Zsh, use `export DbConnection="your_connection_string_here"`)*
        4.  Run the EF Core database update command:
            ```bash
            dotnet ef database update --project .\Data\Data.csproj --startup-project .\Backend\Backend.csproj
            ```

#### Running the Backend

* **Using Visual Studio 2022:**
    1.  Right-click the `Backend` project in Solution Explorer.
    2.  Select "Set as Startup Project."
    3.  Press `F5` or click "Start Debugging."
* **Using VS Code:**
    1.  Open the solution folder in VS Code.
    2.  Go to the "Run and Debug" view (Ctrl+Shift+D).
    3.  Select the debug profile named "Attach to .Net Functions" (or "Launch Functions" if available) and click the green play button.

**Accessing Swagger UI:**

Once the backend is running, you can access the **Swagger UI** for API documentation and testing at:

`http://localhost:7124/api/swagger/ui`

*(Note: The port `7124` is the default for Azure Functions. If your functions run on a different port, adjust the URL accordingly.)*


### 2. Frontend Setup (Next.js - `./Frontend/evn`)

The frontend is a Next.js 15 application.

* **Using VS Code:**
    1.  Open the solution folder in VS Code.
    2.  Open the integrated terminal (Ctrl+`).
    3.  Navigate into the frontend project directory:
        ```bash
        cd Frontend/evn
        ```
    4.  Install npm dependencies:
        ```bash
        npm install
        ```
    5.  **Run the application:**
        * Go to the "Run and Debug" view (Ctrl+Shift+D), set the debugger to "Compound" (if configured for both frontend and backend), and run.
        * Alternatively, run directly from the terminal:
            ```bash
            npm run dev
            ```
    6.  The frontend will typically be available at `http://localhost:3000`.

### 3. Running Tests

Unit tests are available in the project solution.

* **Note:** While unit tests are included, they require further setup and work. I ran out of time :(

---

## ✨ Project Highlights

I particularly enjoyed exploring and implementing **Azure Durable Functions for orchestration**. It was a fascinating experience working with orchestrator functions, which was new to me, and it proved to be an incredibly powerful tool for building resilient and stateful workflows in a serverless environment.

## 🚧 Further Work & Future Enhancements

* **API Security (Critical):** Implement robust authentication and authorization for the API.
    * **Recommendation:** Integrate with Azure Active Directory (Azure AD) using OAuth 2.0. This would involve users signing in via the frontend, receiving an access token (e.g., in a secure cookie), and authenticating this token on the backend for every API request.
* **Comprehensive Testing:**
    * Develop **Integrated Tests** for the API to ensure seamless interaction between components and the database.
    * Implement **End-to-End (E2E) Tests** for the entire application flow, covering both frontend and backend interactions.
    * Create **Unit and Integration Tests** for the React pages and components in the frontend.
* **Automated CI/CD Release Pipeline (Azure DevOps):** Establish continuous integration and continuous deployment for the entire application.
    * **Approach:**
        * **CI:** Use Azure Pipelines to automatically build, run tests, and package the .NET backend (Functions, Data) and the Next.js frontend upon every code commit.
        * **CD:** Define release pipelines to deploy the Azure Functions to an Azure Function App and the Next.js frontend to an Azure Static Web App or a CDN, ensuring seamless and automated updates to production or staging environments.