#!/bin/bash

# Wait for CosmosDB Emulator to be ready
echo "Waiting for CosmosDB emulator to start..."
sleep 20  # Ensure it has time to start

# Create Database and Container
echo "Creating Database and Container..."
curl -X POST \
    -H "x-ms-version: 2018-12-31" \
    -H "Content-Type: application/json" \
    -H "x-ms-date: $(date -u '+%a, %d %b %Y %H:%M:%S GMT')" \
    -H "Authorization: type=aad" \
    -d '{
          "id": "chatHistoryDB"
        }' \
    "https://cosmosdb_emulator:8081/dbs"

curl -X POST \
    -H "x-ms-version: 2018-12-31" \
    -H "Content-Type: application/json" \
    -H "x-ms-date: $(date -u '+%a, %d %b %Y %H:%M:%S GMT')" \
    -H "Authorization: type=aad" \
    -d '{
          "id": "container-1",
          "partitionKey": {
            "paths": ["/id"],
            "kind": "Hash"
          }
        }' \
    "https://cosmosdb_emulator:8081/dbs/chatHistoryDB/colls"

echo "Database and Container created!"
