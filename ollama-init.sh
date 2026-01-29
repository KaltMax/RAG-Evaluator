#!/bin/bash
# Ollama initialization script
# Pulls required models if they don't exist

# Use environment variables with defaults
EMBEDDING_MODEL=${OLLAMA_EMBEDDING_MODEL:-nomic-embed-text-v2-moe}
CHAT_MODEL=${OLLAMA_CHAT_MODEL:-qwen2.5:14b}

echo "Starting Ollama service..."
/bin/ollama serve &
OLLAMA_PID=$!

echo "Waiting for Ollama to be ready..."
sleep 5

echo "Checking and pulling required models..."
echo "Embedding model: $EMBEDDING_MODEL"
echo "Chat model: $CHAT_MODEL"

# Check if embedding model exists
if ! /bin/ollama list | grep -q "$EMBEDDING_MODEL"; then
    echo "Pulling $EMBEDDING_MODEL model..."
    /bin/ollama pull "$EMBEDDING_MODEL"
else
    echo "$EMBEDDING_MODEL model already exists"
fi

# Check if chat model exists
if ! /bin/ollama list | grep -q "$CHAT_MODEL"; then
    echo "Pulling $CHAT_MODEL model..."
    /bin/ollama pull "$CHAT_MODEL"
else
    echo "$CHAT_MODEL model already exists"
fi

echo "Ollama initialization complete!"

# Keep the container running
wait $OLLAMA_PID
