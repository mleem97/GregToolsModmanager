const http = require('http');

const port = process.env.PORT || 8081;

const tools = [
  'list_assemblies',
  'search_types',
  'search_methods',
  'get_type_detail',
  'suggest_greg_api',
  'export_full_scan',
  'get_hook_candidates'
];

const server = http.createServer((req, res) => {
  if (req.url === '/health') {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ status: 'ok', service: 'mcp' }));
    return;
  }

  if (req.url === '/tools') {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ tools }));
    return;
  }

  res.writeHead(200, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify({
    service: 'gregframework-mcp',
    message: 'MCP container is running',
    endpoints: ['/health', '/tools']
  }));
});

server.listen(port, () => {
  console.log(`MCP service listening on ${port}`);
});
