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

# Check if qwen2.5:14b exists
if ! /bin/ollama list | grep -q "qwen2.5:14b"; then
    echo "Pulling Qwen2.5:14B model..."
    /bin/ollama pull qwen2.5:14b
else
    echo "qwen2.5:14b model already exists"
fi

echo "Ollama initialization complete!"

# Keep the container running
wait $OLLAMA_PID
