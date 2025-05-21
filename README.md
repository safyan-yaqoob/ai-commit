# ai_commit

A .NET tool that generates smart, context-aware Git commit messages using the locally running Llama2:7b-chat model via Ollama and Docker. It analyzes your staged changes and suggests a concise, conventional commit message to boost your productivity and keep your commit history clean.

## Setup

### Prerequisites
- **Docker Desktop** (required to run Ollama and the Llama2 model)
- **.NET 8 SDK** (for building and running the C# project)
- **Git** (for version control)

### Steps
1. **Clone this repository**
   ```sh
   git clone <your-repo-url>
   cd ai_commit
   ```
2. **Install Docker Desktop**
   - Download and install from: https://www.docker.com/products/docker-desktop
   - Make sure Docker Desktop is running.

3. **Start Ollama and Llama2 model using Docker Compose**
   - Ensure your `docker-compose.yml` is set up to run the Ollama server.
   - Pull and start the Llama2:7b-chat model (the `init-llama.sh` script can help automate this):
     ```sh
     ./init-llama.sh
     ```
   - This will wait for Ollama to start and pull the Llama2:7b-chat model for you.

4. **Build the .NET project**
   ```sh
   dotnet build ai_commit/ai_commit.csproj
   ```

## How to Run

1. **Stage your changes in your Git repository**
   ```sh
   git add <files>
   ```
2. **Run the commit message generator**
   ```sh
   dotnet run --project ai_commit/ai_commit.csproj
   ```
   - The tool will analyze your staged changes, send them to the Llama2 model, and print a suggested commit message.
   - You can then use this message for your commit:
     ```sh
     git commit -m "<suggested message>"
     ```

## Notes
- The Llama2:7b-chat model is pulled and run locally via Ollama in Docker, so no API keys or cloud access are required.
- The tool uses LibGit2Sharp to analyze staged changes and generate diffs for the AI model.
- Make sure Docker Desktop and the Ollama container are running before using the tool.
