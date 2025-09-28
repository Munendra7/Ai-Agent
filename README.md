# AI-Agent

**AI-Agent** is a full-stack project that leverages **React** for the frontend and **.NET** for the backend to deliver an intelligent, context-aware AI assistant. It supports bringing your own documents and videos for instant contextual knowledge, intelligent RAG, document generation, email workflows, and more.

Designed with **Semantic Kernel’s** orchestration, memory, and planning capabilities, the agent can reason, retrieve knowledge, and execute multi-step actions seamlessly, making it more than just a chatbot.

---

### 📽️ Demo  

![AIAgentDEMOGIF](https://github.com/user-attachments/assets/9dd90e2f-a6e8-4baf-b1d3-5b1d9fedbf7e)

---

---

## 🏗️ Architecture Diagram  

Below is the high-level architecture of the **AI-Agent** project:  

<img width="1490" height="960" alt="diagram-export-8-17-2025-3_07_24-PM" src="https://github.com/user-attachments/assets/a2feea2c-664b-434b-a34f-ffbf0d856daa" />


> The diagram shows how the **React frontend**, **ASP.NET Core backend**, **Semantic Kernel**, **Node.js services**, **Qdrant**, **Azure SQL**, and **Azure Blob Storage** interact together to deliver an intelligent, extensible AI agent.

---

## ✨ Features

- 📄 **Bring Your Own Documents**: Instantly provide context to the AI agent from your files.  
- 🧠 **Intelligent RAG**: Retrieve accurate answers from your knowledge base using Qdrant vector DB.  
- 🎥 **Video-to-Context**: Extract audio from video files using **FFmpeg** and transcribe with **Azure Speech-to-Text API** for contextual Q&A.  
- 🧾 **Template-Based PDF Generation**: Create PDF documents using pre-defined Word templates and dynamic content.  
- 📧 **Draft & Send Emails**: Automatically draft and send emails using contextual data.  
- 💬 **Conversation History**: Maintains a memory of past chats for contextual continuity.  
- ☁️ **Weather API Integration**: Fetch real-time weather updates via a dedicated plugin.  
- 🌐 **Web Search**: Uses Google Search API to retrieve up-to-date information.  
- 🔌 **Plugin-Based Architecture**: Add or update capabilities without disrupting the core.  
- 🤖 **Semantic Kernel Orchestration**: Leverages Microsoft Semantic Kernel for planning, memory, and agent behavior.  
- 🔐 **Authentication & Authorization**: Secure access with MSAL (frontend) and ASP.NET Identity (backend).  

---

## 🛠 Tech Stack

- **Frontend**: React + TailwindCSS + MSAL  
- **Backend**: ASP.NET Core + Semantic Kernel  
- **AI/LLM**: Any LLM (supporting auto function calling)  
- **Vector Store**: Qdrant  
- **Database**: SQL Server  
- **File Storage**: Azure Blob Storage  
- **Video/Audio Processing**: FFmpeg (Node.js service)
- **Containerization**: Docker & Docker Compose  

---

## 📦 Modules & Architecture

- 🎞️ **FFmpeg Node Server**: Extracts audio streams from video files for transcription.
- 📄 **Docx to pdf Node Server**: Converts DOCX to PDF.  
- 🗣️ **Azure Speech-to-Text**: Converts extracted audio into text for knowledge ingestion.  
- 🧠 **Qdrant Vector DB**: Stores embeddings for semantic search and RAG.  
- 🗃️ **SQL Server**: Manages structured relational data and user info.  
- ☁️ **Azure Blob Storage**: Stores uploaded files and processed content.  
- ⚙️ **ASP.NET Backend**: Semantic Kernel-powered orchestrator for AI workflows.  
- 🖥️ **React Frontend**: Clean, responsive UI with TailwindCSS and MSAL auth.  

---

## 🚀 Getting Started

To run the project locally using Docker:

```bash
docker-compose up --build
```

> ⚠️ **Before running, make sure to:**
>
>   - Set the **Tenant ID**
**Frontend**: React + TailwindCSS + MSAL  
**Backend**: ASP.NET Core + Semantic Kernel (using Semantic Kernel Agent Framework)  
**AI/LLM**: Any LLM (supporting auto function calling)  
>   - Configure the **Redirect URIs**
> - 🛠️ **Run the database migration** once the containers are up to apply Entity Framework Core migrations:
> - 🔐 **Authentication supports multiple providers:**
>   - **Microsoft** (MSAL)
>   - **Google**
>   - **Local sign-in**
>   Every logged-in user has their own set of knowledge and templates to manage agent activities independently.
>   - For Microsoft authentication, configure settings in the Azure portal:
>     - Set the **Client ID**
>     - Set the **Tenant ID**
>     - Configure the **Redirect URIs**
>   - For Google and local sign-in, update the relevant environment variables and backend configuration as needed.
> 
> - 🛠️ **Run the database migration** once the containers are up to apply Entity Framework Core migrations:
>   - **Using .NET CLI:**   
>```bash
>dotnet ef database update
>```
>Or using Package Manager Console in Visual Studio:
>```powershell
>Update-Database
>```
 _Run these commands from the ASP.NET Core Web API project directory._