const http = require('http');

const port = process.env.PORT || 8080;

const config = {
  postgresHost: process.env.POSTGRES_HOST || 'postgres',
  postgresDb: process.env.POSTGRES_DB || 'modstore',
  s3Endpoint: process.env.S3_ENDPOINT || 'http://minio:9000',
  s3Bucket: process.env.S3_BUCKET || 'modstore-assets'
};

const server = http.createServer((req, res) => {
  if (req.url === '/health') {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ status: 'ok', service: 'modstore-api' }));
    return;
  }

  if (req.url === '/config') {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify(config));
    return;
  }

  if (req.url === '/mods') {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({
      items: [],
      note: 'Modstore API skeleton is running. Wire DB + S3 logic next.'
    }));
    return;
  }

  res.writeHead(200, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify({
    service: 'datacentermods-modstore-api',
    endpoints: ['/health', '/config', '/mods']
  }));
});

server.listen(port, () => {
  console.log(`Modstore API listening on ${port}`);
});
