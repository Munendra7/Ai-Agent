FROM node:18-slim

# Install ffmpeg
RUN apt-get update && apt-get install -y ffmpeg && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY . .
RUN npm install

VOLUME ["/ffmpeg_data"]
EXPOSE 5000

CMD ["npm", "start"]
