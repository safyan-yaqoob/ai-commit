#!/bin/bash
echo "Waiting for Ollama to start..."
sleep 10

echo "Pulling Llama2 model..."
curl -X POST http://localhost:11434/api/pull -d '{"name": "llama2"}'

echo "Model is ready to use!"
