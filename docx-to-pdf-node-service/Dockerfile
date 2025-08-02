# Use official Node base image (Debian-based)
FROM node:18-slim

# Install LibreOffice
RUN apt-get update && \
    apt-get install -y libreoffice libreoffice-writer && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /app

# Copy files and install dependencies
COPY package*.json ./
RUN npm install
COPY . .

VOLUME ["/docx_uploads"]
VOLUME ["/docx_outputs"]
# Expose the app port
EXPOSE 3000

# Start the service
CMD ["npm", "start"]