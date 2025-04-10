# AI-Agent

**AI-Agent** is a full-stack project that leverages React for the frontend and .NET for the backend to deliver an intelligent, context-aware AI assistant.

[![Watch the demo](https://via.placeholder.com/800x450.png?text=Watch+Demo)](https://drive.google.com/file/d/1BNx342cQbMi0Pnw9BZ0yc1Fk5wX7b6T1/view?usp=sharing)


## ✨ Features

- 🔗 **Web API Integration**: Interacts with external web APIs to fetch or send data.
- 🌐 **Web Search**: Retrieves up-to-date information from the internet.
- 📄 **Document Intelligence**: Understands and extracts knowledge from the documents you provide.
- 🧾 **Template-Based Document Generation**: Creates new documents using provided templates and dynamic content.
- 💬 **Chat History Maintenance**: Remembers previous conversations for better context and continuity.
- 🧠 **Vector Database for Memory**: Uses a vector store to recall information and maintain long-term memory.
- 📧 **Email Drafting and Sending**: Can generate and send emails based on the context.
- 🔌 **Plugin-Based Architecture**: Easily customizable plugin system where you can add or update capabilities.
- 🤖 **Semantic Kernel Integration**: Uses Microsoft Semantic Kernel in the backend for orchestration, memory, planning, and agent-like behavior.
- 🔐 **Authentication & Authorization**: Integrated with Microsoft Identity Platform using MSAL in the frontend and ASP.NET Identity in the backend.

## 🛠 Tech Stack

- **Frontend**: React + MSAL (for authentication)
- **Backend**: ASP.NET Core + Semantic Kernel
- **AI/LLM**: You can use any LLM model. Use GPT-4o for better performance.
- **Vector Store**: Qdrant
- **Database**: SQL Server
- **File Storage**: Azure Blob Storage
- **Containerization**: Docker & Docker Compose

## 📦 Containerized Architecture

This project is fully containerized using Docker. Services include:

- 🧠 **Qdrant**: Vector database for memory storage and retrieval
- 🗃️ **SQL Server**: Relational database for storing structured data and user information
- ☁️ **Azure Blob Storage (emulated/local)**: For document and file handling
- ⚙️ **.NET Backend API**: Semantic Kernel-powered orchestrator and business logic layer
- 🖥️ **React Frontend**: User interface with MSAL-based authentication

## 🚀 Getting Started

To run the project locally using Docker:

```bash
docker-compose up --build
```

> ⚠️ **Before running, make sure to:**
>
> - ✅ **Update the required connection strings and environment variables** in both the backend (`appsettings.json`) and frontend (`.env`) as per your setup.
> - 🔐 **Configure authentication settings properly in the Azure portal** for MSAL to work:
>   - Set the **Client ID**
>   - Set the **Tenant ID**
>   - Configure the **Redirect URIs**
> - 🛠️ **Run the database migration** once the containers are up to apply Entity Framework Core migrations:
>
>   Using .NET CLI:
>   ```bash
>   dotnet ef database update
>   ```
>
>   Or using Package Manager Console in Visual Studio:
>   ```powershell
>   Update-Database
>   ```
>   _Run these commands from the ASP.NET Core Web API project directory._

