#!/bin/bash
# Ollama initialization script
# Pulls required models if they don't exist

# Use environment variables with defaults
EMBEDDING_MODELS=${OLLAMA_EMBEDDING_MODELS:-nomic-embed-text-v2-moe}
CHAT_MODEL=${OLLAMA_CHAT_MODEL:-qwen2.5:14b}

echo "Starting Ollama service..."
/bin/ollama serve &
OLLAMA_PID=$!

echo "Waiting for Ollama to be ready..."
sleep 5

echo "Checking and pulling required models..."
echo "Embedding models: $EMBEDDING_MODELS"
echo "Chat model: $CHAT_MODEL"

# Pull each embedding model
IFS=',' read -ra MODELS <<< "$EMBEDDING_MODELS"
for MODEL in "${MODELS[@]}"; do
    MODEL=$(echo "$MODEL" | xargs)  # trim whitespace
    if ! /bin/ollama list | grep -q "$MODEL"; then
        echo "Pulling embedding model: $MODEL"
        /bin/ollama pull "$MODEL"
    else
        echo "$MODEL model already exists"
    fi
done

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
