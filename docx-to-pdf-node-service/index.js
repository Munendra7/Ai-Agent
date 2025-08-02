const express = require('express');
const multer = require('multer');
const cors = require('cors');
const { exec } = require('child_process');
const path = require('path');
const fs = require('fs');

const app = express();
const port = 3000;

app.use(cors());
const upload = multer({ dest: '/docx_uploads/' });

app.post('/convert', upload.single('file'), (req, res) => {
  const filePath = req.file.path;
  const outputDir = '/docx_outputs';

  exec(`libreoffice --headless --convert-to pdf --outdir ${outputDir} ${filePath}`, (err) => {
    if (err) {
      console.error('Conversion failed:', err);
      return res.status(500).send('Conversion failed');
    }

    const outputFile = path.join(outputDir, path.basename(filePath, '.docx') + '.pdf');
    res.download(outputFile, () => {
      fs.unlinkSync(filePath);
      fs.unlinkSync(outputFile);
    });
  });
});

app.listen(port, () => {
  console.log(`DOCX to PDF API running at http://localhost:${port}`);
});