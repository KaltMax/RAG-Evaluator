#!/bin/bash
# Ollama initialization script
# Pulls required models if they don't exist

echo "Starting Ollama service..."
/bin/ollama serve &
OLLAMA_PID=$!

echo "Waiting for Ollama to be ready..."
sleep 5

echo "Checking and pulling required models..."

# Check if nomic-embed-text exists
if ! /bin/ollama list | grep -q "nomic-embed-text"; then
    echo "Pulling nomic-embed-text model..."
    /bin/ollama pull nomic-embed-text
else
    echo "nomic-embed-text model already exists"
fi

# Check if llama3.2:1b exists
if ! /bin/ollama list | grep -q "llama3.2:1b"; then
    echo "Pulling llama3.2:1b model..."
    /bin/ollama pull llama3.2:1b
else
    echo "llama3.2:1b model already exists"
fi

echo "Ollama initialization complete!"

# Keep the container running
wait $OLLAMA_PID
