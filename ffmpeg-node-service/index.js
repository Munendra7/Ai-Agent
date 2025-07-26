const express = require('express');
const multer = require('multer');
const { v4: uuidv4 } = require('uuid');
const path = require('path');
const { exec } = require('child_process');
const fs = require('fs');

const app = express();
const PORT = 5000;
const uploadDir = '/ffmpeg_data';

if (!fs.existsSync(uploadDir)) fs.mkdirSync(uploadDir);

const storage = multer.diskStorage({
  destination: (req, file, cb) => cb(null, uploadDir),
  filename: (req, file, cb) => cb(null, uuidv4() + path.extname(file.originalname))
});
const upload = multer({ storage });

app.post('/extract-audio', upload.single('file'), (req, res) => {
  if (!req.file) return res.status(400).json({ error: 'No file uploaded' });

  const inputPath = req.file.path;
  const outputPath = inputPath.replace(path.extname(inputPath), '.wav');

  const cmd = `ffmpeg -y -i "${inputPath}" -vn -acodec pcm_s16le -ar 16000 -ac 1 "${outputPath}"`;

  exec(cmd, (error, stdout, stderr) => {
    if (error) {
      console.error(stderr);
      return res.status(500).json({ error: 'FFmpeg failed' });
    }

    res.download(outputPath, (err) => {
      fs.unlinkSync(inputPath);
      fs.unlinkSync(outputPath);
      if (err) console.error('Error sending file:', err);
    });
  });
});

app.listen(PORT, () => {
  console.log(`🎧 FFmpeg Node service running on port ${PORT}`);
});
